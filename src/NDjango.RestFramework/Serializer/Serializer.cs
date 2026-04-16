using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Helpers;
using Newtonsoft.Json;

namespace NDjango.RestFramework.Serializer
{
    public class Serializer<TOrigin, TDestination, TPrimaryKey, TContext>
        where TDestination : BaseModel<TPrimaryKey>
        where TOrigin : BaseDto<TPrimaryKey>
        where TContext : DbContext
    {
        /// <summary>
        /// Cache of per-field validation hook methods, keyed by the concrete serializer type.
        /// Each entry maps a <typeparamref name="TOrigin"/> property name to the <see cref="MethodInfo"/>
        /// of the <c>Validate{PropertyName}Async</c> method on the serializer subclass.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, MethodInfo>> _perFieldHookCache = new();

        /// <summary>
        /// Cache of the generic <see cref="PartialJsonObject{T}.SetValue{R}"/> method definition,
        /// used to write back normalized values during PATCH validation.
        /// </summary>
        private static readonly MethodInfo _setValueOpenMethod = typeof(PartialJsonObject<TOrigin>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == nameof(PartialJsonObject<TOrigin>.SetValue) && m.IsGenericMethodDefinition)
            ?? throw new InvalidOperationException(
                $"Could not locate generic {nameof(PartialJsonObject<TOrigin>)}.{nameof(PartialJsonObject<TOrigin>.SetValue)}<R>(...) method. " +
                "This likely indicates the library's PartialJsonObject API has changed; please file a bug.");

        /// <summary>
        /// Per-property cache of the closed generic <see cref="PartialJsonObject{T}.SetValue{R}"/>
        /// method and the <c>d =&gt; d.Property</c> lambda used to invoke it. Computing these on
        /// every PATCH validation iteration burns CPU and allocates on the hot path; caching them
        /// keyed by <see cref="PropertyInfo"/> makes the cost a one-time startup hit per property.
        /// </summary>
        private static readonly ConcurrentDictionary<PropertyInfo, (MethodInfo ClosedSetValue, object Lambda)> _setValueCache = new();

        /// <summary>
        /// Builds and caches the (closed <see cref="PartialJsonObject{T}.SetValue{R}"/> method,
        /// <c>d =&gt; d.Property</c> lambda) pair for <paramref name="property"/>. The factory is
        /// not static because it captures <see cref="_setValueOpenMethod"/>, a static field of the
        /// closed generic type <see cref="Serializer{TOrigin, TDestination, TPrimaryKey, TContext}"/> —
        /// the capture is benign because both fields share the same generic closure.
        /// </summary>
        internal static (MethodInfo ClosedSetValue, object Lambda) GetSetValueInvoker(PropertyInfo property)
        {
            return _setValueCache.GetOrAdd(property, p =>
            {
                var closedSetValue = _setValueOpenMethod.MakeGenericMethod(p.PropertyType);
                var param = Expression.Parameter(typeof(TOrigin), "d");
                var memberAccess = Expression.Property(param, p);
                var lambda = Expression.Lambda(
                    typeof(Func<,>).MakeGenericType(typeof(TOrigin), p.PropertyType),
                    memberAccess, param);
                return (closedSetValue, (object)lambda);
            });
        }

        /// <summary>
        /// The database context, available to subclasses for custom queries
        /// (e.g., uniqueness checks inside <see cref="ValidateAsync(TOrigin, IDictionary{string, List{string}})"/>).
        /// </summary>
        /// <remarks>
        /// Usage guidelines when called from <c>ValidateAsync</c>:
        /// <list type="bullet">
        /// <item>Always use <c>.AsNoTracking()</c> on read queries — validation should never track entities.</item>
        /// <item>Do NOT call <c>SaveChangesAsync()</c> inside <c>ValidateAsync</c>. The base CRUD method
        /// (<see cref="CreateAsync"/>, <see cref="UpdateAsync"/>, etc.) will save after validation succeeds.
        /// Calling <c>SaveChangesAsync()</c> during validation interacts badly with the change tracker
        /// and may persist entities prematurely.</item>
        /// <item>Do NOT attach, add, or remove entities during validation — only read.</item>
        /// </list>
        /// </remarks>
        protected readonly TContext _dbContext;

        public Serializer(TContext applicationDbContext)
        {
            _dbContext = applicationDbContext;
        }

        /// <summary>
        /// Validates and optionally mutates the incoming DTO before a create operation (POST) or
        /// a bulk update operation (PutMany). Override this method to add business rules that
        /// require async I/O (e.g., database uniqueness checks) or to normalize field values.
        /// Populate <paramref name="errors"/> to signal validation failures; the controller will
        /// short-circuit to 400 when the dictionary is non-empty.
        /// </summary>
        /// <remarks>
        /// This overload is invoked by <b>both POST and PutMany</b>. PutMany does not pass entity IDs
        /// because the same payload applies to many rows — so entity-specific checks (e.g., "uniqueness
        /// excluding all target entities") cannot be expressed here. If your PutMany needs per-entity
        /// validation context, override <see cref="UpdateManyAsync"/> and perform the checks there
        /// before the bulk update. For single-entity PUT, prefer the
        /// <see cref="ValidateAsync(TOrigin, TPrimaryKey, IDictionary{string, List{string}})"/> overload
        /// which receives the entity id.
        /// </remarks>
        /// <param name="data">The DTO to validate. Mutate properties directly to normalize values.</param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not share
        /// across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <returns>The (possibly mutated) DTO that will be forwarded to the CRUD operation.</returns>
        public virtual Task<TOrigin> ValidateAsync(
            TOrigin data,
            IDictionary<string, List<string>> errors)
            => Task.FromResult(data);

        /// <summary>
        /// Validates and optionally mutates the incoming DTO before a full update operation (PUT).
        /// The <paramref name="entityId"/> allows querying the existing entity for skip-self
        /// uniqueness checks (the DRF <c>self.instance</c> equivalent). By default delegates to
        /// the non-ID overload.
        /// </summary>
        /// <param name="data">The DTO to validate.</param>
        /// <param name="entityId">The primary key of the entity being updated.</param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not share
        /// across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <returns>The (possibly mutated) DTO.</returns>
        public virtual Task<TOrigin> ValidateAsync(
            TOrigin data,
            TPrimaryKey entityId,
            IDictionary<string, List<string>> errors)
            => ValidateAsync(data, errors);

        /// <summary>
        /// Validates and optionally mutates the incoming partial DTO before a partial update
        /// operation (PATCH). Use <c>partialData.IsSet(d =&gt; d.Field)</c> to check which fields
        /// were sent and <c>partialData.SetValue(d =&gt; d.Field, value)</c> to normalize values.
        /// </summary>
        /// <param name="partialData">The partial DTO wrapper.</param>
        /// <param name="entityId">The primary key of the entity being patched.</param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not share
        /// across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <returns>The (possibly mutated) partial DTO.</returns>
        public virtual Task<PartialJsonObject<TOrigin>> ValidateAsync(
            PartialJsonObject<TOrigin> partialData,
            TPrimaryKey entityId,
            IDictionary<string, List<string>> errors)
            => Task.FromResult(partialData);

        /// <summary>
        /// Unified cross-field validation hook that receives a <see cref="ValidationContext{TPrimaryKey}"/>
        /// with operation metadata (entity ID, partial flag). Override this to implement cross-field
        /// business rules that apply across POST, PUT, and PATCH without duplicating logic.
        /// This is invoked <b>after</b> all per-field <c>Validate{Property}Async</c> hooks pass
        /// (no errors) and <b>before</b> the legacy <c>ValidateAsync</c> overloads.
        /// </summary>
        /// <param name="data">
        /// The DTO to validate. For PATCH operations, this is the materialized <see cref="PartialJsonObject{T}.Instance"/>.
        /// </param>
        /// <param name="context">
        /// Operation metadata: <see cref="ValidationContext{TPrimaryKey}.EntityId"/>,
        /// <see cref="ValidationContext{TPrimaryKey}.IsCreate"/>,
        /// <see cref="ValidationContext{TPrimaryKey}.IsPartial"/>.
        /// </param>
        /// <param name="errors">Per-field error collector.</param>
        /// <returns>The (possibly mutated) DTO.</returns>
        public virtual Task<TOrigin> ValidateAsync(
            TOrigin data,
            ValidationContext<TPrimaryKey> context,
            IDictionary<string, List<string>> errors)
            => Task.FromResult(data);

        /// <summary>
        /// Discovers per-field validation hooks on the concrete serializer subclass.
        /// A valid hook must be named <c>Validate{PropertyName}Async</c> where PropertyName
        /// matches a property on <typeparamref name="TOrigin"/>, and its signature must be:
        /// <c>Task&lt;TFieldType&gt; Validate{PropertyName}Async(TFieldType, ValidationContext&lt;TPrimaryKey&gt;, IDictionary&lt;string, List&lt;string&gt;&gt;)</c>
        /// where TFieldType matches the property type.
        /// </summary>
        private Dictionary<string, MethodInfo> DiscoverPerFieldHooks()
        {
            return _perFieldHookCache.GetOrAdd(GetType(), serializerType =>
            {
                var hooks = new Dictionary<string, MethodInfo>();
                var originProperties = typeof(TOrigin)
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

                var methods = serializerType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    if (!method.Name.StartsWith("Validate", StringComparison.Ordinal) ||
                        !method.Name.EndsWith("Async", StringComparison.Ordinal))
                        continue;

                    // Extract the property name between "Validate" and "Async"
                    var propertyName = method.Name.Substring("Validate".Length,
                        method.Name.Length - "Validate".Length - "Async".Length);

                    if (string.IsNullOrEmpty(propertyName))
                        continue;

                    if (!originProperties.TryGetValue(propertyName, out var property))
                        continue;

                    // Validate the method signature: Task<TFieldType>(TFieldType, ValidationContext<TPrimaryKey>, IDictionary<string, List<string>>)
                    var parameters = method.GetParameters();
                    if (parameters.Length != 3)
                        continue;

                    var expectedReturnType = typeof(Task<>).MakeGenericType(property.PropertyType);
                    if (method.ReturnType != expectedReturnType)
                        continue;

                    if (parameters[0].ParameterType != property.PropertyType)
                        continue;

                    if (parameters[1].ParameterType != typeof(ValidationContext<TPrimaryKey>))
                        continue;

                    if (parameters[2].ParameterType != typeof(IDictionary<string, List<string>>))
                        continue;

                    hooks[property.Name] = method;
                }

                return hooks;
            });
        }

        /// <summary>
        /// Returns the set of all <c>Validate{X}Async</c> method names on the concrete serializer type
        /// that look like per-field hooks (correct prefix/suffix, 3 params) but whose <c>X</c> does NOT
        /// match any property on <typeparamref name="TOrigin"/>. Used by startup validation to catch
        /// typos like <c>ValidateCnjAsync</c> instead of <c>ValidateCnpjAsync</c>.
        /// </summary>
        internal static IReadOnlyList<string> GetMisnamedHooks(Type serializerType)
        {
            var originProperties = typeof(TOrigin)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // The base Serializer's own ValidateAsync overloads should not be flagged.
            var baseMethods = typeof(Serializer<TOrigin, TDestination, TPrimaryKey, TContext>)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => m.Name)
                .ToHashSet();

            var misnamed = new List<string>();

            foreach (var method in serializerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!method.Name.StartsWith("Validate", StringComparison.Ordinal) ||
                    !method.Name.EndsWith("Async", StringComparison.Ordinal))
                    continue;

                // Skip the base ValidateAsync overloads (including the new context-based one)
                if (baseMethods.Contains(method.Name))
                    continue;

                var propertyName = method.Name.Substring("Validate".Length,
                    method.Name.Length - "Validate".Length - "Async".Length);

                if (string.IsNullOrEmpty(propertyName))
                    continue;

                // Only flag methods that have the 3-parameter hook shape
                var parameters = method.GetParameters();
                if (parameters.Length != 3)
                    continue;

                if (parameters[1].ParameterType != typeof(ValidationContext<TPrimaryKey>))
                    continue;

                if (parameters[2].ParameterType != typeof(IDictionary<string, List<string>>))
                    continue;

                if (!originProperties.Contains(propertyName))
                    misnamed.Add(method.Name);
            }

            return misnamed;
        }

        /// <summary>
        /// Orchestrates the full validation pipeline for POST, PUT, and PATCH operations.
        /// The pipeline mirrors Django REST Framework's validation order:
        /// <list type="number">
        /// <item><b>Per-field hooks</b> — <c>Validate{Property}Async</c> for each property (PATCH: only sent fields).</item>
        /// <item><b>Short-circuit</b> — If per-field hooks populated errors, stop here.</item>
        /// <item><b>Cross-field</b> — <c>ValidateAsync(data, context, errors)</c>.</item>
        /// <item><b>Legacy overloads</b> — The appropriate legacy <c>ValidateAsync</c> overload for backward compat.</item>
        /// </list>
        /// </summary>
        internal async Task<TOrigin> RunValidationAsync(
            TOrigin data,
            ValidationContext<TPrimaryKey> context,
            IDictionary<string, List<string>> errors,
            PartialJsonObject<TOrigin>? partialData = null)
        {
            var hooks = DiscoverPerFieldHooks();
            var originProperties = typeof(TOrigin).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Step 1: Per-field hooks
            foreach (var property in originProperties)
            {
                if (!hooks.TryGetValue(property.Name, out var hookMethod))
                    continue;

                // PATCH: skip fields not present in the partial payload
                if (partialData != null && !partialData.IsSet(property.Name))
                    continue;

                var currentValue = property.GetValue(data);
                var resultTask = (Task)hookMethod.Invoke(this, new[] { currentValue, context, errors });
                await resultTask.ConfigureAwait(false);

                // Extract the result from Task<TFieldType>
                var resultProperty = resultTask.GetType().GetProperty("Result");
                var newValue = resultProperty!.GetValue(resultTask);

                // If the hook returned a different value, write it back
                if (!Equals(currentValue, newValue))
                {
                    if (partialData != null)
                    {
                        // For PATCH: use PartialJsonObject.SetValue<R> via reflection (cached)
                        var (closedSetValue, lambda) = GetSetValueInvoker(property);
                        closedSetValue.Invoke(partialData, new object[] { lambda, newValue });
                        // Re-read the instance since SetValue resets it
                        data = partialData.Instance;
                    }
                    else
                    {
                        property.SetValue(data, newValue);
                    }
                }
            }

            // Step 2: Short-circuit if per-field hooks added errors
            if (errors.Count > 0)
                return data;

            // Step 3: Unified cross-field ValidateAsync
            data = await ValidateAsync(data, context, errors).ConfigureAwait(false);

            // Write back to partial if PATCH and cross-field mutated
            if (partialData != null)
            {
                // Sync any mutations from cross-field back to partial's underlying data
                foreach (var property in originProperties)
                {
                    if (partialData.IsSet(property.Name))
                    {
                        var (closedSetValue, lambda) = GetSetValueInvoker(property);
                        var currentVal = property.GetValue(data);
                        closedSetValue.Invoke(partialData, new object[] { lambda, currentVal });
                    }
                }
            }

            // Step 4: Legacy overloads for backward compatibility
            switch (context.Operation)
            {
                case SerializerOperation.Create:
                case SerializerOperation.BulkUpdate:
                    data = await ValidateAsync(data, errors).ConfigureAwait(false);
                    break;
                case SerializerOperation.Update:
                    data = await ValidateAsync(data, context.EntityId!, errors).ConfigureAwait(false);
                    break;
                case SerializerOperation.PartialUpdate:
                    await ValidateAsync(partialData!, context.EntityId!, errors).ConfigureAwait(false);
                    break;
            }

            return data;
        }

        /// <summary>
        /// Overload of <see cref="RunValidationAsync"/> for PATCH operations that accepts a
        /// <see cref="PartialJsonObject{T}"/> and returns the (possibly mutated) partial wrapper.
        /// </summary>
        internal async Task<PartialJsonObject<TOrigin>> RunValidationForPartialAsync(
            PartialJsonObject<TOrigin> partialData,
            ValidationContext<TPrimaryKey> context,
            IDictionary<string, List<string>> errors)
        {
            await RunValidationAsync(partialData.Instance, context, errors, partialData).ConfigureAwait(false);
            return partialData;
        }

        public virtual async Task<TDestination> CreateAsync(TOrigin data)
        {
            var stringDeserialized = JsonConvert.SerializeObject(data);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);

            await _dbContext.Set<TDestination>().AddAsync(destinationObject);
            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<TDestination> PartialUpdateAsync(PartialJsonObject<TOrigin> originObject, TPrimaryKey entityId)
        {
            var destinationObject = await GetFromDB(entityId);

            if (destinationObject == null)
                return null;

            var destinationType = typeof(TDestination);

            foreach (var property in typeof(TOrigin).GetProperties())
            {
                if (property.PropertyType.IsEnum)
                    throw new NotImplementedException("Lists are not supported");

                if (originObject.IsSet(property.Name))
                {
                    var productProperty = destinationType.GetProperty(property.Name);
                    productProperty.SetValue(destinationObject, property.GetValue(originObject.Instance));
                }
            }

            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<TDestination> UpdateAsync(TOrigin origin, TPrimaryKey entityId)
        {
            var destinationObject = await GetFromDB(entityId);

            if (destinationObject == null)
                return null;

            var stringDeserialized = JsonConvert.SerializeObject(origin);

            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);
            stringDeserializedDynamic.Id = entityId;

            JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), destinationObject);
            _dbContext.Update(destinationObject);
            await _dbContext.SaveChangesAsync();

            return destinationObject;
        }

        public virtual async Task<IList<TPrimaryKey>> UpdateManyAsync(TOrigin origin, IList<TPrimaryKey> entityIds)
        {
            var destinationObjects = await GetManyFromDB(entityIds);

            var stringDeserialized = JsonConvert.SerializeObject(origin);
            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);

            foreach (var obj in destinationObjects)
            {
                stringDeserializedDynamic.Id = obj.Id;
                JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), obj);
            }

            _dbContext.UpdateRange(destinationObjects);
            await _dbContext.SaveChangesAsync();

            return destinationObjects.Select(m => m.Id).ToList();
        }

        public virtual async Task<TDestination> DestroyAsync(TPrimaryKey entityId)
        {
            var data = await GetFromDB(entityId);

            if (data == null)
                return null;

            _dbContext.Remove(data);
            await _dbContext.SaveChangesAsync();

            return data;
        }

        public virtual async Task<IList<TPrimaryKey>> DestroyManyAsync(IList<TPrimaryKey> entityIds)
        {
            var deletedObjects = await GetManyFromDB(entityIds);

            _dbContext.RemoveRange(deletedObjects);
            await _dbContext.SaveChangesAsync();

            return deletedObjects.Select(m => m.Id).ToList();
        }

        public async Task<TDestination> GetFromDB(TPrimaryKey id, IQueryable<TDestination> query)
        {
            var key = id.ToString();
            var data = await query.Where(x => x.Id.ToString() == key).FirstOrDefaultAsync();

            return data;
        }

        protected async Task<TDestination> GetFromDB(TPrimaryKey id)
        {
            return await _dbContext.Set<TDestination>().FindAsync(id);
        }

        protected async Task<IList<TDestination>> GetManyFromDB(IList<TPrimaryKey> entityIds)
        {
            return await _dbContext.Set<TDestination>().Where(m => entityIds.Contains(m.Id)).ToListAsync();
        }
    }
}
