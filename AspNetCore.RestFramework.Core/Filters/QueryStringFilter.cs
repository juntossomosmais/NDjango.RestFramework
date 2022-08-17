using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace AspNetCore.RestFramework.Core.Filters
{
    public class QueryStringFilter<TEntity> : Filter<TEntity>
    {
        private readonly string[] _allowedFields;

        public QueryStringFilter(string[] allowedFields)
        {
            _allowedFields = allowedFields;
        }

        public override IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
        {
            return Builder(query, request);
        }

        private IQueryable<TEntity> Builder(IQueryable<TEntity> query, HttpRequest httpRequest)
        {
            Dictionary<string, string> fieldsToFilter = new Dictionary<string, string>();

            foreach (var item in httpRequest.Query)
            {
                var allowedField = _allowedFields.FirstOrDefault(f => f.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
                if (allowedField != null)
                    fieldsToFilter.Add(allowedField, item.Value);
            }

            foreach (var queryItem in fieldsToFilter)
            {
                query = query.Where(GetColumnEquality(queryItem.Key, queryItem.Value));
            }

            return query;
        }

        private static Expression<Func<TEntity, bool>> GetColumnEquality(string property, string term)
        {
            /*
             * Modified the following solution to support Guid parsing:
             * https://stackoverflow.com/questions/17832989/linq-iqueryable-generic-filter/17833880#17833880
             * */

            var obj = Expression.Parameter(typeof(TEntity), "obj");
            
            var objProperty = Expression.PropertyOrField(obj, property);

            object convertedValue;
            if (objProperty.Type.IsAssignableTo(typeof(Guid)))
                convertedValue = Guid.Parse(term);
            else
                convertedValue = Convert.ChangeType(term, objProperty.Type);

            var objEquality = Expression.Equal(objProperty, Expression.Constant(convertedValue));

            var lambda = Expression.Lambda<Func<TEntity, bool>>(objEquality, obj);

            return lambda;
        }
    }
}
