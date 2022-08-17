using AspNetCore.RestFramework.Core.Base;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace AspNetCore.RestFramework.Core.Filters
{
    public class QueryStringIdRangeFilter<TEntity, TPrimaryKey> : Filter<TEntity>
        where TEntity : BaseModel<TPrimaryKey>
    {
        public override IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
        {
            var idsFilter = request.Query.FirstOrDefault(m => m.Key.Equals("ids", StringComparison.OrdinalIgnoreCase));
            var ids = idsFilter.Value.Select(ConvertToPrimaryKeyType).ToList();

            if (ids.Count> 0)
                query = query.Where(m => ids.Contains(m.Id));

            return query;
        }
        
        private static TPrimaryKey ConvertToPrimaryKeyType(string value)
        {
            try
            {
                if (typeof(TPrimaryKey).IsAssignableTo(typeof(Guid)))
                    return (TPrimaryKey)(object)Guid.Parse(value);

                return (TPrimaryKey)Convert.ChangeType(value, typeof(TPrimaryKey));
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid value '{value}' for type {typeof(TPrimaryKey).FullName}", nameof(value), ex);
            }
        }
    }
}
