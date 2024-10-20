using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using NDjango.RestFramework.Base;

namespace NDjango.RestFramework.Filters
{
    public class QueryStringIdRangeFilter<TEntity, TPrimaryKey> : Filter<TEntity>
        where TEntity : BaseModel<TPrimaryKey>
    {
        public override IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
        {
            var idsFilter = request.Query.FirstOrDefault(m => m.Key.Equals("ids", StringComparison.OrdinalIgnoreCase));
            List<TPrimaryKey> ids = null;
            try
            {
                if (idsFilter.Value.Count == 1)
                {
                    var providedIds = idsFilter.Value[0];
                    if (providedIds.StartsWith("["))
                        providedIds = providedIds.Trim('[', ']');
                    ids = providedIds.Split(',').Select(ConvertToPrimaryKeyType).ToList();
                }
            }
            catch (Exception)
            {
                // ignored
            }
            if (ids is null)
                ids = idsFilter.Value.Select(ConvertToPrimaryKeyType).ToList();

            if (ids.Count > 0)
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
