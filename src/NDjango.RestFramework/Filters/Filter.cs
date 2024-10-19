using System.Linq;
using Microsoft.AspNetCore.Http;

namespace NDjango.RestFramework.Filters
{
    public abstract class Filter<TEntity>
    {
        /// <summary>
        /// Adds filters to the query based on the HTTP request given its query parameters, for example.
        /// </summary>
        /// <param name="query">The provided query.</param>
        /// <param name="request">The HTTP request containing query parameters, headers, etc.</param>
        /// <returns>A modified query with applied filters.</returns>
        public abstract IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request);
    }
}
