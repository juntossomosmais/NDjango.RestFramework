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

        public QueryStringFilter(string[] allowedFilters)
        {
            _allowedFields = allowedFilters;
        }

        public override IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
        {
            return Builder(query, request);
        }

        private IQueryable<T> Builder<T>(IQueryable<T> query, HttpRequest httpRequest)
        {
            Dictionary<string, string> fieldsToFilter = new Dictionary<string, string>();

            foreach (var item in httpRequest.Query)
            {
                var allowedField = _allowedFields.FirstOrDefault(f => f.Equals(item.Key, StringComparison.OrdinalIgnoreCase));
                if (allowedField != null)
                    fieldsToFilter.Add(allowedField, item.Value);
            }

            List<Expression<Func<T, bool>>> lst = new List<Expression<Func<T, bool>>>();

            foreach (var queryItem in fieldsToFilter)
            {
                query = query.Where(GetColumnEquality<T>(queryItem.Key, queryItem.Value));
            }

            return query;
        }

        private static Expression<Func<T, bool>> GetColumnEquality<T>(string property, string term)
        {
            /*
             * Modified the following solution to support Guid parsing:
             * https://stackoverflow.com/questions/17832989/linq-iqueryable-generic-filter/17833880#17833880
             * */

            var obj = Expression.Parameter(typeof(T), "obj");
            
            var objProperty = Expression.PropertyOrField(obj, property);

            object convertedValue;
            if (objProperty.Type.IsAssignableTo(typeof(Guid)))
                convertedValue = Guid.Parse(term);
            else
                convertedValue = Convert.ChangeType(term, objProperty.Type);

            var objEquality = Expression.Equal(objProperty, Expression.Constant(convertedValue));

            var lambda = Expression.Lambda<Func<T, bool>>(objEquality, obj);

            return lambda;
        }
    }
}
