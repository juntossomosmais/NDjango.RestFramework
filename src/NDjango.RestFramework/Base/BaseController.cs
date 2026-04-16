using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public virtual async Task<IActionResult> GetSingle([FromRoute] TPrimaryKey id)
        {
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);

            var data = await _serializer.GetFromDB(id, query);
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
        public async Task<IActionResult> ListPaged()
        {
            var query = FilterQuery(GetQuerySet(), HttpContext.Request);
            query = SortQuery(AllowedFields, query);
            var paginated = await _pagination.PaginateAsync(query, HttpContext.Request);
            if (paginated == null)
                return NotFound();

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
        public virtual async Task<IActionResult> Post([FromBody] TOrigin entity)
        {
            var errors = new Dictionary<string, List<string>>();
            entity = await _serializer.ValidateAsync(entity, errors);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            var data = await _serializer.CreateAsync(entity);

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return CreatedAtAction(nameof(GetSingle), new { id = data.Id }, jObject);
        }

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
            [FromRoute] TPrimaryKey id)
        {
            if (!_actionOptions.AllowPatch)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var errors = new Dictionary<string, List<string>>();
            entity = await _serializer.ValidateAsync(entity, id, errors);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            var data = await _serializer.PartialUpdateAsync(entity, id);

            if (data == null)
                return NotFound();

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return Ok(jObject);
        }

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
            [FromRoute] TPrimaryKey id)
        {
            if (!_actionOptions.AllowPut)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var errors = new Dictionary<string, List<string>>();
            origin = await _serializer.ValidateAsync(origin, id, errors);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            var data = await _serializer.UpdateAsync(origin, id);

            if (data == null)
                return NotFound();

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return Ok(jObject);
        }

        /// <summary>
        /// Fully updates multiple entities.
        /// </summary>
        /// <param name="origin">The entity data to update.</param>
        /// <param name="ids">The primary keys of the entities to update.</param>
        /// <returns>The list of updated entity IDs.</returns>
        [HttpPut]
        public virtual async Task<IActionResult> PutMany(
            [FromBody] TOrigin origin,
            [FromQuery] IList<TPrimaryKey> ids)
        {
            if (!_actionOptions.AllowPut)
                return StatusCode(StatusCodes.Status405MethodNotAllowed);

            var errors = new Dictionary<string, List<string>>();
            origin = await _serializer.ValidateAsync(origin, errors);
            if (errors.Count > 0)
                return BadRequest(new ValidationErrors(ToValidationErrorsDict(errors)));

            var updatedIds = await _serializer.UpdateManyAsync(origin, ids);

            return Ok(updatedIds);
        }

        /// <summary>
        /// Deletes an entity by its primary key.
        /// </summary>
        /// <param name="id">The primary key of the entity to delete.</param>
        /// <returns>The deleted entity if found, otherwise a NotFound result.</returns>
        [HttpDelete]
        [Route("{id}")]
        public virtual async Task<IActionResult> Delete([FromRoute] TPrimaryKey id)
        {
            var data = await _serializer.DestroyAsync(id);

            if (data == null)
                return NotFound();

            var json = JsonConvert.SerializeObject(
                data,
                new JsonSerializerSettings { ContractResolver = new JsonTransform(_fieldsToBeRendered) }
            );

            var jObject = JObject.Parse(json);
            return Ok(jObject);
        }

        /// <summary>
        /// Deletes multiple entities by their primary keys.
        /// </summary>
        /// <param name="ids">The primary keys of the entities to delete.</param>
        /// <returns>The list of deleted entity IDs.</returns>
        [HttpDelete]
        public virtual async Task<IActionResult> DeleteMany([FromQuery] IList<TPrimaryKey> ids)
        {
            var deletedIds = await _serializer.DestroyManyAsync(ids);

            return Ok(deletedIds);
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

        #endregion
    }
}
