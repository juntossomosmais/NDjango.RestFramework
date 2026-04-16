using System.Collections.Generic;

namespace NDjango.RestFramework.Helpers
{
    /// <summary>
    /// Public extension methods used by <c>ValidateAsync</c> overrides on <see cref="Serializer.Serializer{TOrigin,TDestination,TPrimaryKey,TContext}"/>
    /// to build the per-field error dictionary ergonomically.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Returns the existing list for the given key, or creates a new one, adds it to the
        /// dictionary, and returns it. Intended for building per-field error lists in
        /// <c>ValidateAsync</c> overrides without boilerplate TryGetValue/init/add code.
        /// </summary>
        public static List<string> GetOrAdd(
            this IDictionary<string, List<string>> dict, string key)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<string>();
                dict[key] = list;
            }
            return list;
        }
    }
}
