using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
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
        /// The database context, available to subclasses for custom queries (e.g., uniqueness
        /// checks inside <see cref="ValidateAsync"/> or per-field <c>Validate{Property}Async</c>
        /// hooks).
        /// </summary>
        /// <remarks>
        /// Usage guidelines when called during validation:
        /// <list type="bullet">
        /// <item>Always use <c>.AsNoTracking()</c> on read queries — validation should never track entities.</item>
        /// <item>Do NOT call <c>SaveChangesAsync()</c> during validation. The CRUD method
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
        /// Cross-field validation hook invoked for POST, PUT, PATCH, and PutMany. Override
        /// this to implement business rules that span more than one field; for single-field
        /// rules, prefer per-field <c>Validate{PropertyName}Async</c> hooks (auto-discovered
        /// by convention).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Pipeline order: per-field hooks run first; if any populated <paramref name="errors"/>,
        /// this method is <b>not</b> called and the controller short-circuits to 400.
        /// </para>
        /// <para>
        /// On PATCH, <paramref name="data"/> is the materialized
        /// <see cref="PartialJsonObject{T}.Instance"/> — properties absent from the request body
        /// hold their default value. Use <see cref="ValidationContext{TPrimaryKey}.IsSet"/> to
        /// distinguish "not sent" from "sent as the default value".
        /// </para>
        /// </remarks>
        /// <param name="data">The DTO to validate (mutate to normalize values).</param>
        /// <param name="context">
        /// Operation metadata: <see cref="ValidationContext{TPrimaryKey}.Operation"/>,
        /// <see cref="ValidationContext{TPrimaryKey}.EntityId"/> (for Update / PartialUpdate),
        /// <see cref="ValidationContext{TPrimaryKey}.IsSet"/> (PATCH presence).
        /// </param>
        /// <param name="errors">
        /// Per-field error collector. Populate via <c>errors.GetOrAdd("Field").Add("message")</c>.
        /// <b>Not thread-safe</b> — write from the <c>ValidateAsync</c> method body only; do not
        /// share across parallel subtasks (e.g., <c>Task.WhenAll</c>) without external synchronization.
        /// </param>
        /// <param name="cancellationToken">
        /// Flowed from <see cref="Microsoft.AspNetCore.Http.HttpContext.RequestAborted"/>. Pass it
        /// to any EF Core async call this hook makes (e.g., <c>FirstOrDefaultAsync(ct)</c>).
        /// </param>
        /// <returns>The (possibly mutated) DTO.</returns>
        public virtual Task<TOrigin> ValidateAsync(
            TOrigin data,
            ValidationContext<TPrimaryKey> context,
            IDictionary<string, List<string>> errors,
            CancellationToken cancellationToken = default)
            => Task.FromResult(data);

        /// <summary>
        /// Discovers per-field validation hooks on the concrete serializer subclass.
        /// A valid hook must be named <c>Validate{PropertyName}Async</c> where PropertyName
        /// matches a property on <typeparamref name="TOrigin"/>, and its signature must be:
        /// <c>Task&lt;TFieldType&gt; Validate{PropertyName}Async(TFieldType, ValidationContext&lt;TPrimaryKey&gt;, IDictionary&lt;string, List&lt;string&gt;&gt;, CancellationToken)</c>
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

                    // Validate the method signature: Task<TFieldType>(TFieldType, ValidationContext<TPrimaryKey>, IDictionary<string, List<string>>, CancellationToken)
                    var parameters = method.GetParameters();
                    if (parameters.Length != 4)
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

                    if (parameters[3].ParameterType != typeof(CancellationToken))
                        continue;

                    hooks[property.Name] = method;
                }

                return hooks;
            });
        }

        /// <summary>
        /// Returns the set of all <c>Validate{X}Async</c> method names on the concrete serializer type
        /// that look like per-field hooks (correct prefix/suffix, 4 params) but whose <c>X</c> does NOT
        /// match any property on <typeparamref name="TOrigin"/>. Used by startup validation to catch
        /// typos like <c>ValidateCnjAsync</c> instead of <c>ValidateCnpjAsync</c>.
        /// </summary>
        internal static IReadOnlyList<string> GetMisnamedHooks(Type serializerType)
        {
            var originProperties = typeof(TOrigin)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // The base Serializer's own ValidateAsync method should not be flagged.
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

                // Skip the base ValidateAsync method itself
                if (baseMethods.Contains(method.Name))
                    continue;

                var propertyName = method.Name.Substring("Validate".Length,
                    method.Name.Length - "Validate".Length - "Async".Length);

                if (string.IsNullOrEmpty(propertyName))
                    continue;

                // Only flag methods that have the 4-parameter hook shape
                var parameters = method.GetParameters();
                if (parameters.Length != 4)
                    continue;

                if (parameters[1].ParameterType != typeof(ValidationContext<TPrimaryKey>))
                    continue;

                if (parameters[2].ParameterType != typeof(IDictionary<string, List<string>>))
                    continue;

                if (parameters[3].ParameterType != typeof(CancellationToken))
                    continue;

                if (!originProperties.Contains(propertyName))
                    misnamed.Add(method.Name);
            }

            return misnamed;
        }

        /// <summary>
        /// Orchestrates the validation pipeline for POST, PUT, PATCH, and PutMany. Mirrors
        /// Django REST Framework's order:
        /// <list type="number">
        /// <item><b>Per-field hooks</b> — <c>Validate{Property}Async</c> for each property (PATCH: only sent fields).</item>
        /// <item><b>Short-circuit</b> — if per-field hooks populated errors, the cross-field hook is not called.</item>
        /// <item><b>Cross-field</b> — <see cref="ValidateAsync"/>.</item>
        /// </list>
        /// </summary>
        internal async Task<TOrigin> RunValidationAsync(
            TOrigin data,
            ValidationContext<TPrimaryKey> context,
            IDictionary<string, List<string>> errors,
            PartialJsonObject<TOrigin>? partialData = null,
            CancellationToken cancellationToken = default)
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
                var resultTask = (Task)hookMethod.Invoke(this, new[] { currentValue, context, errors, (object)cancellationToken });
                await resultTask.ConfigureAwait(false);

                // Extract the result from Task<TFieldType>
                var resultProperty = resultTask.GetType().GetProperty("Result");
                var newValue = resultProperty!.GetValue(resultTask);

                // Write the (possibly normalized) value back so PATCH's underlying JSON and
                // the materialized DTO stay in sync, and POST/PUT/Bulk see the normalized DTO.
                if (!Equals(currentValue, newValue))
                {
                    if (partialData != null)
                    {
                        // PATCH: use PartialJsonObject.SetValue<R> via reflection (cached). This
                        // also invalidates the materialized Instance cache so the next read sees
                        // the normalized value.
                        var (closedSetValue, lambda) = GetSetValueInvoker(property);
                        closedSetValue.Invoke(partialData, new object[] { lambda, newValue });
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
            data = await ValidateAsync(data, context, errors, cancellationToken).ConfigureAwait(false);

            // For PATCH, sync any cross-field mutations back into the partial's underlying JSON
            // so PartialUpdateAsync sees them when it walks the IsSet/Instance pair.
            if (partialData != null)
            {
                foreach (var property in originProperties)
                {
                    if (!partialData.IsSet(property.Name))
                        continue;

                    var (closedSetValue, lambda) = GetSetValueInvoker(property);
                    var currentVal = property.GetValue(data);
                    closedSetValue.Invoke(partialData, new object[] { lambda, currentVal });
                }
            }

            return data;
        }

        public virtual async Task<TDestination> CreateAsync(TOrigin data, CancellationToken cancellationToken = default)
        {
            var stringDeserialized = JsonConvert.SerializeObject(data);
            var destinationObject = JsonConvert.DeserializeObject<TDestination>(stringDeserialized);

            await _dbContext.Set<TDestination>().AddAsync(destinationObject, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return destinationObject;
        }

        public virtual async Task<TDestination?> PartialUpdateAsync(
            PartialJsonObject<TOrigin> originObject,
            TPrimaryKey entityId,
            CancellationToken cancellationToken = default)
        {
            var destinationObject = await GetFromDB(entityId, cancellationToken);

            if (destinationObject == null)
                return null;

            var destinationType = typeof(TDestination);

            foreach (var property in typeof(TOrigin).GetProperties())
            {
                if (originObject.IsSet(property.Name))
                {
                    var productProperty = destinationType.GetProperty(property.Name);
                    productProperty.SetValue(destinationObject, property.GetValue(originObject.Instance));
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return destinationObject;
        }

        public virtual async Task<TDestination?> UpdateAsync(
            TOrigin origin,
            TPrimaryKey entityId,
            CancellationToken cancellationToken = default)
        {
            var destinationObject = await GetFromDB(entityId, cancellationToken);

            if (destinationObject == null)
                return null;

            var stringDeserialized = JsonConvert.SerializeObject(origin);

            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);
            stringDeserializedDynamic.Id = entityId;

            JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), destinationObject);
            _dbContext.Update(destinationObject);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return destinationObject;
        }

        public virtual async Task<IList<TPrimaryKey>> UpdateManyAsync(
            TOrigin origin,
            IList<TPrimaryKey> entityIds,
            CancellationToken cancellationToken = default)
        {
            var destinationObjects = await GetManyFromDB(entityIds, cancellationToken);

            var stringDeserialized = JsonConvert.SerializeObject(origin);
            dynamic stringDeserializedDynamic = JsonConvert.DeserializeObject<dynamic>(stringDeserialized);

            foreach (var obj in destinationObjects)
            {
                stringDeserializedDynamic.Id = obj.Id;
                JsonConvert.PopulateObject(stringDeserializedDynamic.ToString(), obj);
            }

            _dbContext.UpdateRange(destinationObjects);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return destinationObjects.Select(m => m.Id).ToList();
        }

        public virtual async Task<TDestination?> DestroyAsync(TPrimaryKey entityId, CancellationToken cancellationToken = default)
        {
            var data = await GetFromDB(entityId, cancellationToken);

            if (data == null)
                return null;

            _dbContext.Remove(data);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return data;
        }

        public virtual async Task<IList<TPrimaryKey>> DestroyManyAsync(
            IList<TPrimaryKey> entityIds,
            CancellationToken cancellationToken = default)
        {
            var deletedObjects = await GetManyFromDB(entityIds, cancellationToken);

            _dbContext.RemoveRange(deletedObjects);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return deletedObjects.Select(m => m.Id).ToList();
        }

        public async Task<TDestination?> GetFromDB(
            TPrimaryKey id,
            IQueryable<TDestination> query,
            CancellationToken cancellationToken = default)
        {
            var key = id.ToString();
            var data = await query.Where(x => x.Id.ToString() == key).FirstOrDefaultAsync(cancellationToken);

            return data;
        }

        protected async Task<TDestination?> GetFromDB(TPrimaryKey id, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TDestination>().FindAsync(new object?[] { id }, cancellationToken);
        }

        protected async Task<IList<TDestination>> GetManyFromDB(
            IList<TPrimaryKey> entityIds,
            CancellationToken cancellationToken = default)
        {
            return await _dbContext.Set<TDestination>()
                .Where(m => entityIds.Contains(m.Id))
                .ToListAsync(cancellationToken);
        }
    }
}
