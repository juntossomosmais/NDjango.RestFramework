using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Filters;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Paginations;
using NDjango.RestFramework.Serializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NDjango.RestFramework.Base
{
    [Produces("application/json")]
    public abstract class BaseController<TOrigin, TDestination, TPrimaryKey, TContext>
        : ControllerBase, IFieldConfigurableController
        where TOrigin : BaseDto<TPrimaryKey>
        where TDestination : BaseModel<TPrimaryKey>
        where TContext : DbContext
    {
        /// <summary>
        /// Serializer instance available for derived controllers that override actions
        /// and need direct access to data operations.
        /// </summary>
        protected readonly Serializer<TOrigin, TDestination, TPrimaryKey, TContext> _serializer;

        /// <summary>
        /// Logger instance available for derived controllers to log within overridden actions.
        /// </summary>
        protected readonly ILogger _logger;
        private readonly TContext _context;
        private readonly ActionOptions _actionOptions;
        private readonly IPagination<TDestination> _pagination;
        private readonly string[] _fieldsToBeRendered;

        private static readonly Lazy<string[]> _cachedFields = new(ResolveAndValidateFields);
        private static readonly ConcurrentDictionary<Type, bool> _validatedAllowedFields = new();

        public IQueryable<TDestination> Query { get; set; }
        public List<Filter<TDestination>> Filters { get; set; } = new();
        public string[] AllowedFields { get; set; } = [];

        #region Constructors

        protected BaseController(
            Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer,
            TContext context,
            ActionOptions actionOptions,
            ILogger<TDestination> logger,
            IPagination<TDestination>? pagination = null)
        {
            _serializer = serializer;
            _context = context;
            _actionOptions = actionOptions ?? new ActionOptions();
            _logger = logger;
            _pagination = pagination ?? new PageNumberPagination<TDestination>();
            Query = new FilterBuilder<TContext, TDestination>(_context).DbSet;
            _fieldsToBeRendered = _cachedFields.Value;
        }

        protected BaseController(
            Serializer<TOrigin, TDestination, TPrimaryKey, TContext> serializer,
            TContext context,
            ILogger<TDestination> logger,
            IPagination<TDestination>? pagination = null)
            : this(serializer, context, new ActionOptions(), logger, pagination)
        { }

        #endregion

        #region Actions

        /// <summary>
        /// Retrieves a single entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the entity.</param>
        /// <returns>The entity if found, otherwise a NotFound result.</returns>
        [HttpGet]
        [Route("{id}")]
        public virtual async Task<IActionResult> GetSingle(
            [FromRoute] TPrimaryKey id,
            CancellationToken cancellationToken = default)
        {
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);

            var data = await _serializer.GetObjectAsync(query, id, cancellationToken);
            if (data == null)
                return NotFound();

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return Ok(jObject);
        }

        [HttpGet]
        public async Task<IActionResult> ListPaged(CancellationToken cancellationToken = default)
        {
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);
            query = SortQuery(AllowedFields, query);
            // An empty result set is a legitimate success — same posture as DRF's
            // ListModelMixin (rest_framework/mixins.py:34-44 + pagination.py:220-226 at
            // encode/django-rest-framework@3.17.1). The paginator returns an empty envelope;
            // the controller renders it through the same path as a populated page.
            var paginated = await _pagination.PaginateAsync(query, HttpContext.Request, cancellationToken);

            var paginatedResultsAsJson = JsonConvert.SerializeObject(
                paginated.Results,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var results = JArray.Parse(paginatedResultsAsJson);
            var paginatedToBeReturned =
                new PaginatedResponse<JArray>(paginated.Count, paginated.Next, paginated.Previous, results);

            return Ok(paginatedToBeReturned);
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        /// <param name="entity">The entity to create.</param>
        /// <returns>The created entity.</returns>
        [HttpPost]
        public virtual async Task<IActionResult> Post(
            [FromBody] TOrigin entity,
            CancellationToken cancellationToken = default)
        {
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<TPrimaryKey>(SerializerOperation.Create, default);
            entity = await _serializer.RunValidationAsync(entity, context, errors, cancellationToken: cancellationToken);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            var data = await PerformCreateAsync(entity, cancellationToken);

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return CreatedAtAction(nameof(GetSingle), new { id = data.Id }, jObject);
        }

        /// <summary>
        /// Create-time extension point invoked after validation succeeds. The default
        /// delegates to <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.CreateAsync"/>;
        /// override to wrap the persistence in a transaction, write to an outbox, or attach
        /// auditing metadata derived from <see cref="ControllerBase.HttpContext"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Return contract:</b> must return a non-null entity with a populated primary key.
        /// The surrounding <see cref="Post"/> action passes the returned entity's <c>Id</c> to
        /// <see cref="ControllerBase.CreatedAtAction(string?, object?, object?)"/> to build the
        /// <c>Location</c> header — a null return throws <see cref="NullReferenceException"/> at
        /// the action site, not at the override. The non-nullable return type
        /// (<c>Task&lt;TDestination&gt;</c>) is the compile-time encoding of this contract.
        /// </para>
        /// <para>
        /// Tracks DRF's <c>perform_create(serializer)</c> in intent, not signature: this hook
        /// receives the validated <typeparamref name="TOrigin"/> rather than the serializer,
        /// because the serializer is a constructor-injected DI service exposed as the
        /// protected <c>_serializer</c> field. To inject request-derived fields (e.g., audit
        /// metadata from <see cref="ControllerBase.HttpContext"/>) <i>before</i> persistence,
        /// mutate <paramref name="data"/> in the override and then delegate to <c>base</c>.
        /// Override <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.CreateAsync"/>
        /// instead when the logic is shared with non-HTTP callers.
        /// </para>
        /// <para>
        /// Mutations to <paramref name="data"/> in this override replay on every retry — keep
        /// them idempotent if the override wraps <c>base</c> in a retry loop. Field-level
        /// <c>Validate{Field}Async</c> normalization runs once in the surrounding action and
        /// does not re-run on retries.
        /// </para>
        /// <para>
        /// Executes on the request thread with the request-scoped, non-thread-safe
        /// <typeparamref name="TContext"/>; do not parallelize work that touches
        /// <c>_serializer</c> or the <see cref="DbContext"/> from the override
        /// (<c>Parallel.ForEachAsync</c>, <c>Task.WhenAll</c> over EF calls, etc.) —
        /// EF Core throws <see cref="InvalidOperationException"/> on overlapping operations
        /// against a single context instance.
        /// </para>
        /// </remarks>
        protected virtual Task<TDestination> PerformCreateAsync(
            TOrigin data,
            CancellationToken cancellationToken)
            => _serializer.CreateAsync(data, cancellationToken);

        /// <summary>
        /// Partially updates an existing entity.
        /// </summary>
        /// <param name="entity">The partial entity data to update.</param>
        /// <param name="id">The primary key of the entity to update.</param>
        /// <returns>The updated entity if found, otherwise a NotFound result.</returns>
        [HttpPatch]
        [Route("{id}")]
        public virtual async Task<IActionResult> Patch(
            [FromBody] PartialJsonObject<TOrigin> entity,
            [FromRoute] TPrimaryKey id,
            CancellationToken cancellationToken = default)
        {
            if (!_actionOptions.AllowPatch)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var errors = new Dictionary<string, List<string>>();
            // Pass the partial as the presence probe so ValidationContext.IsSet(field) on PATCH
            // forwards to PartialJsonObject.IsSet — distinguishing "not sent" from "sent default".
            var context = new ValidationContext<TPrimaryKey>(SerializerOperation.PartialUpdate, id, entity);
            await _serializer.RunValidationAsync(entity.Instance, context, errors, entity, cancellationToken);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            // Filter-scoped load: row-scoping (tenant, soft-delete, multi-account) happens here,
            // before the hook runs. Out-of-scope ids resolve to null and surface as 404 — the
            // same outcome as a missing row, with no information leak. Mirrors DRF mixins.py:58-67
            // at tag 3.17.1: view does get_object(), then hands the instance to the serializer.
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);
            var instance = await _serializer.GetObjectAsync(query, id, cancellationToken);
            if (instance is null)
                return NotFound();

            var data = await PerformPartialUpdateAsync(instance, entity, cancellationToken);

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return Ok(jObject);
        }

        /// <summary>
        /// Partial-update extension point invoked after validation succeeds and after the
        /// controller has loaded the target entity through its filter chain. The default
        /// delegates to <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.PartialUpdateAsync"/>;
        /// override to wrap the persistence in a transaction, write to an outbox, or attach
        /// auditing metadata derived from <see cref="ControllerBase.HttpContext"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Mirrors DRF's <c>perform_update(serializer)</c> at <c>rest_framework/mixins.py:71-72</c>
        /// (tag 3.17.1). DRF covers both PUT and PATCH with a single hook; this framework
        /// already separates the actions, so the seams are split. The hook receives the
        /// already-loaded <paramref name="instance"/> plus the <see cref="PartialJsonObject{T}"/>
        /// (carrying the per-field <c>IsSet</c> probe). To inject request-derived fields before
        /// persistence, set them on <paramref name="data"/> via
        /// <see cref="PartialJsonObject{T}.SetValue{TValue}"/> in the override before delegating
        /// to <c>base</c>, or mutate <paramref name="instance"/> directly. Override
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.PartialUpdateAsync"/>
        /// instead when the logic is shared with non-HTTP callers.
        /// </para>
        /// <para>
        /// Filter-scoping has already happened at the action's load step before this hook
        /// fires: the controller resolved the id against <see cref="FilterQuery"/> composed
        /// over <see cref="GetQuerySet"/>, so <paramref name="instance"/> is guaranteed to be
        /// a row the caller is allowed to mutate. Overrides that need to enforce further
        /// instance-shaped predicates (e.g., "only allow update if the row is in a state the
        /// caller can transition out of") should validate <paramref name="instance"/> here
        /// before delegating to <c>base</c>.
        /// </para>
        /// <para>
        /// Mutations to <paramref name="data"/> or <paramref name="instance"/> in this override
        /// replay on every retry — keep them idempotent if the override wraps <c>base</c> in a
        /// retry loop. Field-level <c>Validate{Field}Async</c> normalization runs once in the
        /// surrounding action and does not re-run on retries.
        /// </para>
        /// <para>
        /// Executes on the request thread with the request-scoped, non-thread-safe
        /// <typeparamref name="TContext"/>; do not parallelize work that touches
        /// <c>_serializer</c> or the <see cref="DbContext"/> from the override
        /// (<c>Parallel.ForEachAsync</c>, <c>Task.WhenAll</c> over EF calls, etc.) —
        /// EF Core throws <see cref="InvalidOperationException"/> on overlapping operations
        /// against a single context instance.
        /// </para>
        /// </remarks>
        protected virtual Task<TDestination> PerformPartialUpdateAsync(
            TDestination instance,
            PartialJsonObject<TOrigin> data,
            CancellationToken cancellationToken)
            => _serializer.PartialUpdateAsync(instance, data, cancellationToken);

        /// <summary>
        /// Fully updates an existing entity.
        /// </summary>
        /// <param name="origin">The entity data to update.</param>
        /// <param name="id">The primary key of the entity to update.</param>
        /// <returns>The updated entity if found, otherwise a NotFound result.</returns>
        [HttpPut]
        [Route("{id}")]
        public virtual async Task<IActionResult> Put(
            [FromBody] TOrigin origin,
            [FromRoute] TPrimaryKey id,
            CancellationToken cancellationToken = default)
        {
            if (!_actionOptions.AllowPut)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<TPrimaryKey>(SerializerOperation.Update, id);
            origin = await _serializer.RunValidationAsync(origin, context, errors, cancellationToken: cancellationToken);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            // Filter-scoped load: row-scoping (tenant, soft-delete, multi-account) happens here,
            // before the hook runs. Out-of-scope ids resolve to null and surface as 404 — the
            // same outcome as a missing row, with no information leak. Mirrors DRF mixins.py:58-67
            // at tag 3.17.1: view does get_object(), then hands the instance to the serializer.
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);
            var instance = await _serializer.GetObjectAsync(query, id, cancellationToken);
            if (instance is null)
                return NotFound();

            var data = await PerformUpdateAsync(instance, origin, cancellationToken);

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return Ok(jObject);
        }

        /// <summary>
        /// Full-update extension point invoked after validation succeeds and after the
        /// controller has loaded the target entity through its filter chain. The default
        /// delegates to <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.UpdateAsync"/>;
        /// override to wrap the persistence in a transaction, write to an outbox, or attach
        /// auditing metadata derived from <see cref="ControllerBase.HttpContext"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Mirrors DRF's <c>perform_update(serializer)</c> at <c>rest_framework/mixins.py:71-72</c>
        /// (tag 3.17.1). The hook receives the already-loaded <paramref name="instance"/> and
        /// the validated <paramref name="data"/>. To inject request-derived fields before
        /// persistence, mutate <paramref name="data"/> or <paramref name="instance"/> in the
        /// override and then delegate to <c>base</c>. Override
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.UpdateAsync"/>
        /// instead when the logic is shared with non-HTTP callers.
        /// </para>
        /// <para>
        /// Filter-scoping has already happened at the action's load step before this hook
        /// fires: the controller resolved the id against <see cref="FilterQuery"/> composed
        /// over <see cref="GetQuerySet"/>, so <paramref name="instance"/> is guaranteed to be
        /// a row the caller is allowed to mutate. Overrides that need to enforce further
        /// instance-shaped predicates (e.g., "only allow update if the row is in a state the
        /// caller can transition out of") should validate <paramref name="instance"/> here
        /// before delegating to <c>base</c>.
        /// </para>
        /// <para>
        /// Mutations to <paramref name="data"/> or <paramref name="instance"/> in this override
        /// replay on every retry — keep them idempotent if the override wraps <c>base</c> in a
        /// retry loop. Field-level <c>Validate{Field}Async</c> normalization runs once in the
        /// surrounding action and does not re-run on retries.
        /// </para>
        /// <para>
        /// Executes on the request thread with the request-scoped, non-thread-safe
        /// <typeparamref name="TContext"/>; do not parallelize work that touches
        /// <c>_serializer</c> or the <see cref="DbContext"/> from the override
        /// (<c>Parallel.ForEachAsync</c>, <c>Task.WhenAll</c> over EF calls, etc.) —
        /// EF Core throws <see cref="InvalidOperationException"/> on overlapping operations
        /// against a single context instance.
        /// </para>
        /// </remarks>
        protected virtual Task<TDestination> PerformUpdateAsync(
            TDestination instance,
            TOrigin data,
            CancellationToken cancellationToken)
            => _serializer.UpdateAsync(instance, data, cancellationToken);

        /// <summary>
        /// Deletes an entity by its primary key. Returns 204 No Content on success.
        /// Disable this endpoint via <see cref="ActionOptions.AllowDelete"/>; disabled hits
        /// return <c>405 Method Not Allowed</c>.
        /// </summary>
        /// <param name="id">The primary key of the entity to delete.</param>
        /// <returns>
        /// 204 No Content on success, 404 Not Found when the id does not resolve, or
        /// 400 Bad Request with a <see cref="ValidationErrors"/> envelope when
        /// <see cref="ValidateDestroyAsync"/> populates the errors dictionary.
        /// </returns>
        [HttpDelete]
        [Route("{id}")]
        public virtual async Task<IActionResult> Delete(
            [FromRoute] TPrimaryKey id,
            CancellationToken cancellationToken = default)
        {
            if (!_actionOptions.AllowDelete)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var query = FilterQuery(GetQuerySet(), HttpContext.Request);
            var instance = await _serializer.GetObjectAsync(query, id, cancellationToken);
            if (instance is null)
                return NotFound();

            var errors = new Dictionary<string, List<string>>();
            await ValidateDestroyAsync(instance, errors, cancellationToken);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            await PerformDestroyAsync(instance, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Delete-time validation seam invoked between fetch and remove. Override to enforce
        /// state predicates ("address is main and others exist", "store has open orders") that
        /// can't be expressed as input validation. The actual delete-site extension point is
        /// <see cref="PerformDestroyAsync"/> on the controller (or
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyAsync(TDestination, CancellationToken)"/>
        /// on the serializer when the logic is shared with non-HTTP callers).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Populate <paramref name="errors"/> to short-circuit the request with a 400 carrying
        /// the library's <c>ValidationErrors</c> envelope. Use
        /// <see cref="ValidationErrors.NonFieldErrorsKey"/> for object-level errors that don't
        /// belong to a specific field.
        /// </para>
        /// <para>
        /// <b>Validate only — do not perform side effects here.</b> The framework does not open
        /// a transaction around this call. Outbox writes, message publishing, and transactional
        /// wrapping belong in <see cref="PerformDestroyAsync"/> or in the serializer's
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyAsync(TDestination, CancellationToken)"/>
        /// override, not in this hook. The authoritative re-check (under row lock or via
        /// <c>RowVersion</c>) likewise belongs in the persistence-site override.
        /// </para>
        /// <para>
        /// Executes on the request thread with the request-scoped, non-thread-safe
        /// <typeparamref name="TContext"/>; do not parallelize work that touches
        /// <c>_serializer</c> or the <see cref="DbContext"/> from the override.
        /// </para>
        /// </remarks>
        protected virtual Task ValidateDestroyAsync(
            TDestination instance,
            IDictionary<string, List<string>> errors,
            CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Delete extension point invoked after <see cref="ValidateDestroyAsync"/> succeeds.
        /// Mirrors DRF's <c>perform_destroy(self, instance)</c> at
        /// <c>rest_framework/mixins.py:88-89</c> (tag 3.17.1); the default delegates to
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyAsync(TDestination, CancellationToken)"/>.
        /// Override to wrap the delete in a transaction, write to an outbox, publish a domain
        /// event, or attach audit metadata derived from <see cref="ControllerBase.HttpContext"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Filter-scoping has already happened at the action's load step before this hook
        /// fires: the controller resolved the id against <see cref="FilterQuery"/> composed
        /// over <see cref="GetQuerySet"/>, so <paramref name="instance"/> is guaranteed to be
        /// a row the caller is allowed to delete. Overrides that need to enforce further
        /// instance-shaped predicates should validate in <see cref="ValidateDestroyAsync"/>
        /// (which short-circuits to 400) rather than here.
        /// </para>
        /// <para>
        /// Override this on the <b>controller</b> when the side effect is request-shaped —
        /// audit metadata from <see cref="ControllerBase.HttpContext"/>, request-scoped tracing.
        /// Override
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyAsync(TDestination, CancellationToken)"/>
        /// on the <b>serializer</b> when the logic is shared with non-HTTP callers.
        /// </para>
        /// <para>
        /// Executes on the request thread with the request-scoped, non-thread-safe
        /// <typeparamref name="TContext"/>; do not parallelize work that touches
        /// <c>_serializer</c> or the <see cref="DbContext"/> from the override
        /// (<c>Parallel.ForEachAsync</c>, <c>Task.WhenAll</c> over EF calls, etc.).
        /// </para>
        /// </remarks>
        protected virtual Task PerformDestroyAsync(
            TDestination instance,
            CancellationToken cancellationToken)
            => _serializer.DestroyAsync(instance, cancellationToken);

        /// <summary>
        /// Deletes multiple entities by their primary keys.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This action calls
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyManyAsync"/>,
        /// which uses a single <c>ExecuteDeleteAsync</c> SQL statement and therefore <b>does
        /// not invoke <see cref="ValidateDestroyAsync"/> nor any override of
        /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.DestroyAsync(TDestination, CancellationToken)"/></b>
        /// — see that method's remarks for the full bypass list (per-row validation, EF
        /// <c>SaveChanges</c> interceptors, audit-on-delete hooks, soft-delete logic).
        /// Disable this endpoint via <see cref="ActionOptions.AllowBulkDelete"/> when those
        /// hooks carry rules the bulk path must not silently skip.
        /// </para>
        /// </remarks>
        /// <param name="ids">The primary keys of the entities to delete.</param>
        /// <returns>204 No Content on success.</returns>
        [HttpDelete]
        public virtual async Task<IActionResult> DeleteMany(
            [FromQuery] IList<TPrimaryKey> ids,
            CancellationToken cancellationToken = default)
        {
            if (!_actionOptions.AllowBulkDelete)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var query = FilterQuery(GetQuerySet(), HttpContext.Request);
            await _serializer.DestroyManyAsync(query, ids, cancellationToken);

            return NoContent();
        }

        #endregion

        #region Utils

        private static IDictionary<string, string[]> ToValidationErrorsDict(
            IDictionary<string, List<string>> errors)
            => errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray());

        private static string[] ResolveAndValidateFields()
        {
            // Activator.CreateInstance is used here because GetFields() is an instance method on BaseModel<T>.
            // This runs once per closed generic type (cached via Lazy<>), and EF Core already requires
            // a parameterless constructor, so the practical cost is zero. Migrating to static abstract
            // would require an interface (C# only allows static abstract in interfaces, not abstract classes),
            // an extra generic constraint, and a breaking change for every consumer — not worth it
            // unless bundled with a major version bump.
            var instance = (TDestination)Activator.CreateInstance(typeof(TDestination), null);
            var fields = instance.GetFields();

            var properties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertyNames = properties.Select(p => p.Name).ToArray();
            var invalidFields = new List<string>();

            foreach (var field in fields)
            {
                if (field.Contains(':'))
                {
                    var parts = field.Split(':', 2);
                    var typeName = parts[0];
                    var nestedPropertyName = parts[1];

                    var navigationProperty = properties.FirstOrDefault(p =>
                    {
                        var elementType = GetElementType(p.PropertyType);
                        return string.Equals(elementType.Name, typeName, StringComparison.OrdinalIgnoreCase);
                    });

                    if (navigationProperty == null)
                    {
                        invalidFields.Add(field);
                        continue;
                    }

                    var navType = GetElementType(navigationProperty.PropertyType);
                    var navProperties = navType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    var hasNestedProperty = navProperties.Any(p =>
                        string.Equals(p.Name, nestedPropertyName, StringComparison.OrdinalIgnoreCase));

                    if (!hasNestedProperty)
                        invalidFields.Add(field);
                }
                else
                {
                    var hasProperty = propertyNames.Any(p =>
                        string.Equals(p, field, StringComparison.OrdinalIgnoreCase));

                    if (!hasProperty)
                        invalidFields.Add(field);
                }
            }

            if (invalidFields.Count > 0)
            {
                throw new InvalidOperationException(
                    $"{typeof(TDestination).Name}.GetFields() contains invalid fields: [{string.Join(", ", invalidFields)}]. " +
                    $"Valid properties: [{string.Join(", ", propertyNames)}].");
            }

            return fields;
        }

        private static Type GetElementType(Type type)
        {
            if (type == typeof(string))
                return type;

            var enumerableInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableInterface != null)
                return enumerableInterface.GetGenericArguments()[0];

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            return type;
        }

        private static void ValidateAllowedFields(string[] allowedFields)
        {
            var properties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var propertyNames = properties.Select(p => p.Name).ToArray();

            var invalidFields = allowedFields
                .Where(field => !propertyNames.Any(p => string.Equals(p, field, StringComparison.OrdinalIgnoreCase)))
                .ToArray();

            if (invalidFields.Length > 0)
            {
                throw new InvalidOperationException(
                    $"AllowedFields for {typeof(TDestination).Name} contains invalid fields: [{string.Join(", ", invalidFields)}]. " +
                    $"Valid properties: [{string.Join(", ", propertyNames)}].");
            }
        }

        [NonAction]
        public virtual IQueryable<TDestination> FilterQuery(IQueryable<TDestination> query, HttpRequest request)
        {
            EnsureAllowedFieldsValidated();

            foreach (var filter in Filters)
                query = filter.AddFilter(query, request);
            return query;
        }

        [NonAction]
        public virtual IQueryable<TDestination> SortQuery(string[] allowedFilters, IQueryable<TDestination> query)
        {
            EnsureAllowedFieldsValidated();

            return new SortFilter<TDestination>().Sort(query, HttpContext.Request, allowedFilters);
        }

        private void EnsureAllowedFieldsValidated()
        {
            if (_validatedAllowedFields.TryGetValue(GetType(), out _))
                return;

            if (AllowedFields.Length > 0)
                ValidateAllowedFields(AllowedFields);

            _validatedAllowedFields.TryAdd(GetType(), true);
        }

        [NonAction]
        public virtual IQueryable<TDestination> GetQuerySet()
        {
            return Query ?? new FilterBuilder<TContext, TDestination>(_context).DbSet;
        }

        #endregion

        #region IFieldConfigurableController

        string[] IFieldConfigurableController.GetFieldsConfiguration() => _fieldsToBeRendered;
        string[] IFieldConfigurableController.GetAllowedFieldsConfiguration() => AllowedFields;
        Type IFieldConfigurableController.GetDestinationType() => typeof(TDestination);
        IReadOnlyList<string> IFieldConfigurableController.GetMisnamedValidationHooks()
            => Serializer<TOrigin, TDestination, TPrimaryKey, TContext>.GetMisnamedHooks(_serializer.GetType());

        #endregion
    }
}
