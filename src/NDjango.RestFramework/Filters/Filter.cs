using Microsoft.AspNetCore.Http;
using System.Linq;

namespace NDjango.RestFramework.Filters
{
    public abstract class Filter<TEntity>
    {
        public abstract IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request);
    }
}
