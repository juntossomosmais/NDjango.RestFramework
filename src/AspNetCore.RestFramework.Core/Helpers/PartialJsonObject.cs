using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AspNetCore.RestFramework.Core.Helpers
{
    [JsonConverter(typeof(PartialJsonObjectConverter))]
    public class PartialJsonObject<T> : PartialJsonObject where T : class
    {
        [JsonIgnore]
        private T _instance;
        [JsonIgnore]
        public T Instance => _instance ?? (_instance = ToObject());

        [JsonIgnore]
        private Dictionary<string, bool> _cache = new Dictionary<string, bool>();

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
