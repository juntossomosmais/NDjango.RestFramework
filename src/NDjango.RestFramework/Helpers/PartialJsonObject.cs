using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NDjango.RestFramework.Helpers
{
    /// <remarks>
    /// This type is not thread-safe. Do not invoke <see cref="SetValue{R}"/>, <see cref="IsSet(string)"/>,
    /// <see cref="Add{R}"/>, <see cref="Remove{R}"/>, or any other member from concurrent tasks on the same
    /// instance — the internal JObject, cache dictionary, and materialized instance cache are all mutated
    /// without synchronization. Each request receives its own instance via model binding, so this is only
    /// a concern if a <c>ValidateAsync</c> override launches parallel subtasks (e.g., <c>Task.WhenAll</c>)
    /// that touch the same partial object.
    /// </remarks>
    [JsonConverter(typeof(PartialJsonObjectConverter))]
    public class PartialJsonObject<T> : PartialJsonObject where T : class
    {
        [JsonIgnore]
        private T _instance;
        [JsonIgnore]
        public T Instance => _instance ?? (_instance = ToObject());

        [JsonIgnore]
        private readonly Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        public PartialJsonObject(JObject jsonObject)
        {
            JsonObject = jsonObject;
        }

        public PartialJsonObject(string json)
        {
            JsonObject = JObject.Parse(json);
        }

        public bool IsSet<R>(Expression<Func<T, R>> expPath)
        {
            var paths = ExpressionToPathParser.ParseExpressionToPaths(expPath.Body).ToArray();

            return IsSet(paths);
        }

        public override bool IsSet(string path)
        {
            return IsSet(path.Split('.'));
        }

        public override bool IsSet(params string[] paths)
        {
            var path = string.Join(".", paths).ToLower();

            if (_cache.TryGetValue(path, out var isSet))
            {
                return isSet;
            }

            isSet = TryGetFullPath(JsonObject, paths, 0, out var _);

            _cache[path] = isSet;

            if (isSet && paths.Length > 1)
            {
                for (var i = 1; i < paths.Length; i++)
                {
                    path = string.Join(".", paths.Take(i)).ToLower();

                    _cache[path] = true;
                }
            }

            return isSet;
        }

        /// <summary>
        /// Get the value if set, if not get the default value
        /// </summary>
        /// <typeparam name="R">Type of the return value</typeparam>
        /// <param name="path">Value path</param>
        /// <param name="defaultValue">Default value. This is returned in case if the searched field was not found</param>
        /// <returns>If the field has a populated value, it returns the field and if not, returns the default value</returns>
        public R GetIfSet<R>(Expression<Func<T, R>> path, R defaultValue = default)
        {
            return IsSet(path) ? path.Compile()(Instance) : defaultValue;
        }

        /// <summary>
        /// Sets or replaces the value of a property in the underlying JSON object. This is
        /// intended for use inside <c>ValidateAsync</c> overrides to normalize field values
        /// before the partial update is applied. After calling this method, <see cref="IsSet"/>
        /// will return <c>true</c> for the property and the materialized <see cref="Instance"/>
        /// will reflect the new value.
        /// </summary>
        /// <typeparam name="R">The type of the property.</typeparam>
        /// <param name="property">An expression identifying the property to set (e.g., <c>d =&gt; d.CNPJ</c>).</param>
        /// <param name="value">The new value to assign.</param>
        public void SetValue<R>(Expression<Func<T, R>> property, R value)
        {
            var memberPath = GetMemberPath(property);
            var paths = memberPath.Split('.');
            var jsonPath = GetJSONPath(paths);

            var token = JsonObject.SelectToken(jsonPath);
            var newValue = value == null ? JValue.CreateNull() : JToken.FromObject(value);

            if (token != null)
            {
                token.Replace(newValue);
            }
            else
            {
                JsonObject[paths[0]] = paths.Length == 1
                    ? newValue
                    : throw new NotSupportedException(
                                    $"SetValue cannot create nested properties that are absent from the incoming JSON. " +
                                    $"Path '{memberPath}' is nested and not present. Only top-level properties can be added.");
            }

            _instance = null;
            _cache[memberPath.ToLower()] = true;
        }

        public PartialJsonObject<T> Add<R>(Expression<Func<T, R>> expPath)
        {
            var path = GetMemberPath(expPath).ToLower();

            _cache.Remove(path);
            _cache.Add(path, true);

            return this;
        }

        public PartialJsonObject<T> Remove<R>(Expression<Func<T, R>> expPath)
        {
            var path = GetMemberPath(expPath).ToLower();

            _cache.Remove(path);
            _cache.Add(path, false);

            return this;
        }

        public string GetJSONPath<R>(Expression<Func<T, R>> expPath)
        {
            var paths = ExpressionToPathParser.ParseExpressionToPaths(expPath.Body).ToArray();

            return GetJSONPath(paths);
        }

        public string GetJSONPath(params string[] paths)
        {
            if (TryGetFullPath(JsonObject, paths, 0, out var fullPath))
            {
                return fullPath;
            }

            return string.Join(".", paths);
        }

        public void ClearCache() => _cache.Clear();

        public void CopyTo(T obj)
        {
            JsonConvert.PopulateObject(JsonObject.ToString(), obj);
        }

        /// <summary>
        /// Cache of (sourceDtoType, targetEntityType) → property pairs eligible for copy. Each
        /// entry is built once via reflection on first use and reused across all subsequent calls.
        /// </summary>
        private static readonly ConcurrentDictionary<(Type Source, Type Target), CopyPair[]> _applyToCache = new();

        /// <summary>
        /// Copies the fields <em>actually present</em> in this partial onto <paramref name="entity"/>,
        /// returning the names that were applied. Intended to replace the
        /// <c>if (originObject.IsSet(...)) entity.X = originObject.Instance.X;</c> ladder consumers
        /// write in <see cref="Serializer.Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.PartialUpdateAsync"/>
        /// overrides — and to give them the list of applied field names for outbox payload construction.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A property is copied when <b>all</b> of these hold:
        /// </para>
        /// <list type="bullet">
        ///   <item>Source DTO and target entity have a public instance property with the same name.</item>
        ///   <item>The target property has a setter.</item>
        ///   <item>Source-property type is assignment-compatible with the target-property type
        ///     (treating <c>Nullable&lt;X&gt;</c> on either side as equivalent to <c>X</c>).</item>
        ///   <item>The property name is not in <paramref name="except"/>.</item>
        /// </list>
        /// <para>
        /// Anything else is silently skipped — the caller can inspect the returned list to see which
        /// names were actually applied and handle the remainder manually (computed fields, renames,
        /// type-converted fields, etc.).
        /// </para>
        /// </remarks>
        /// <param name="entity">The target entity. Must be non-null.</param>
        /// <param name="except">Property names to skip even when otherwise eligible. Use <c>nameof()</c>.</param>
        /// <returns>
        /// The property names that were copied onto <paramref name="entity"/>, in declaration order
        /// of the source DTO. Useful for populating an outbox payload from exactly the changed fields.
        /// </returns>
        public IReadOnlyList<string> ApplyTo<TEntity>(TEntity entity, params string[] except)
            where TEntity : class
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var exceptSet = (except is null || except.Length == 0)
                ? null
                : new HashSet<string>(except, StringComparer.Ordinal);

            var pairs = _applyToCache.GetOrAdd(
                (typeof(T), typeof(TEntity)),
                key => BuildCopyPairs(key.Source, key.Target));

            var instance = Instance;
            var applied = new List<string>(pairs.Length);

            foreach (var pair in pairs)
            {
                if (!IsSet(pair.Name))
                    continue;
                if (exceptSet != null && exceptSet.Contains(pair.Name))
                    continue;

                var value = pair.Source.GetValue(instance);
                pair.Target.SetValue(entity, value);
                applied.Add(pair.Name);
            }

            return applied;
        }

        private static CopyPair[] BuildCopyPairs(Type sourceType, Type targetType)
        {
            var sourceProps = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var targetByName = targetType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.Ordinal);

            var pairs = new List<CopyPair>(sourceProps.Length);
            foreach (var src in sourceProps)
            {
                if (!targetByName.TryGetValue(src.Name, out var tgt))
                    continue;
                if (!IsAssignmentCompatible(src.PropertyType, tgt.PropertyType))
                    continue;
                pairs.Add(new CopyPair(src.Name, src, tgt));
            }
            return pairs.ToArray();
        }

        /// <summary>
        /// Permissive type-compatibility check: a source property of type <c>X</c> can fill a target
        /// of type <c>X</c>, <c>X?</c>, or any base/interface of <c>X</c>; nullable wrappers on either
        /// side are treated transparently. Reference-type assignments rely on runtime null safety
        /// (we don't enforce that a nullable source actually has a non-null value at copy time —
        /// that's the consumer's invariant).
        /// </summary>
        private static bool IsAssignmentCompatible(Type from, Type to)
        {
            if (to.IsAssignableFrom(from))
                return true;

            var fromUnwrapped = Nullable.GetUnderlyingType(from) ?? from;
            var toUnwrapped = Nullable.GetUnderlyingType(to) ?? to;
            return toUnwrapped.IsAssignableFrom(fromUnwrapped);
        }

        private readonly struct CopyPair
        {
            public string Name { get; }
            public PropertyInfo Source { get; }
            public PropertyInfo Target { get; }

            public CopyPair(string name, PropertyInfo source, PropertyInfo target)
            {
                Name = name;
                Source = source;
                Target = target;
            }
        }

        public T ToObject()
        {
            return JsonObject.ToObject<T>();
        }

        public static implicit operator T(PartialJsonObject<T> partialJsonObject)
            => partialJsonObject.Instance;

        public static string GetMemberPath<R>(Expression<Func<T, R>> expPath)
        {
            return string.Join(".", ExpressionToPathParser.ParseExpressionToPaths(expPath.Body));
        }

        private static bool TryGetFullPath(JToken token, string[] paths, int pathIndex, out string fullPath)
        {
            var path = paths[pathIndex];
            if (token is JArray array)
            {
                int arrayIndex;

                if (path == "$last")
                    arrayIndex = array.Count - 1;
                else if (!int.TryParse(path, out arrayIndex))
                    arrayIndex = -1;

                if (arrayIndex >= 0 &&
                    arrayIndex < array.Count)
                {
                    pathIndex++;
                    if (pathIndex < paths.Length)
                    {
                        return TryGetFullPath(array[arrayIndex], paths, pathIndex, out fullPath);
                    }

                    fullPath = $"{token.Path}[{arrayIndex}]";
                    return true;
                }
            }
            else if (token is JObject obj)
            {
                var val = obj.GetValue(path, StringComparison.OrdinalIgnoreCase);

                if (val != null)
                {
                    pathIndex++;
                    if (pathIndex < paths.Length)
                    {
                        return TryGetFullPath(val, paths, pathIndex, out fullPath);
                    }

                    fullPath = val.Path;
                    return true;
                }
            }

            fullPath = null;
            return false;
        }
    }

    public abstract class PartialJsonObject
    {
        [JsonIgnore]
        public JObject JsonObject { get; internal set; }

        public abstract bool IsSet(string path);

        public abstract bool IsSet(params string[] paths);
    }

    internal class ExpressionToPathParser
    {
        internal static IEnumerable<string> ParseExpressionToPaths(Expression expressionBody)
        {
            if (expressionBody is MethodCallExpression methodExpr)
            {
                foreach (var p in ParseMethodCallExpressionToPaths(methodExpr))
                    yield return p;
            }
            else if (expressionBody is BinaryExpression binaryExpr &&
                binaryExpr.NodeType == ExpressionType.ArrayIndex)
            {
                foreach (var p in ParseExpressionToPaths(binaryExpr.Left))
                    yield return p;

                yield return GetExpressionCompiledValue(binaryExpr.Right).ToString();
            }
            else if (expressionBody is MemberExpression propExpr)
            {
                foreach (var p in ParseExpressionToPaths(propExpr.Expression))
                    yield return p;

                yield return propExpr.Member.Name;
            }
        }

        internal static IEnumerable<string> ParseMethodCallExpressionToPaths(MethodCallExpression expression)
        {
            if (expression.Method.DeclaringType == typeof(Enumerable) &&
                (expression.Method.Name == nameof(Enumerable.ElementAt) ||
                 expression.Method.Name == nameof(Enumerable.ElementAtOrDefault) ||
                (expression.Method.Name == nameof(Enumerable.First) && expression.Arguments.Count == 1) ||
                (expression.Method.Name == nameof(Enumerable.FirstOrDefault) && expression.Arguments.Count == 1) ||
                (expression.Method.Name == nameof(Enumerable.Last) && expression.Arguments.Count == 1) ||
                (expression.Method.Name == nameof(Enumerable.LastOrDefault) && expression.Arguments.Count == 1)))
            {
                foreach (var p in ParseExpressionToPaths(expression.Arguments[0]))
                    yield return p;

                if (expression.Method.Name == nameof(Enumerable.First) ||
                    expression.Method.Name == nameof(Enumerable.FirstOrDefault))
                    yield return "0";
                else if (expression.Method.Name == nameof(Enumerable.Last) ||
                    expression.Method.Name == nameof(Enumerable.LastOrDefault))
                    yield return "$last";
                else
                    yield return GetExpressionCompiledValue(expression.Arguments[1]).ToString();
            }
            else if (expression.Method.DeclaringType.IsGenericType &&
                expression.Method.DeclaringType == typeof(List<>).MakeGenericType(expression.Method.DeclaringType.GenericTypeArguments[0]) &&
                expression.Method.Name == "get_Item")
            {
                foreach (var p in ParseExpressionToPaths(expression.Object))
                    yield return p;

                yield return GetExpressionCompiledValue(expression.Arguments[0]).ToString();
            }
        }

        private static object GetExpressionCompiledValue(Expression expression)
        {
            if (expression is ConstantExpression constExpr)
                return constExpr.Value;
            else
                return Expression.Lambda(expression).Compile().DynamicInvoke();
        }
    }

    internal class PartialJsonObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => typeof(PartialJsonObject).IsAssignableFrom(objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            return Activator.CreateInstance(objectType, obj);
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }
}
