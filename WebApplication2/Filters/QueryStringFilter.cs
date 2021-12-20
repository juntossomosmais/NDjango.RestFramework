using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using WebApplication2.Controllers;
using WebApplication2.Models;

namespace WebApplication2.Filters
{
    public class QueryStringFilter<Tcontext, TEntity> : Filter<TEntity>
    {
        private string[] _allowedFields;

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
                if (_allowedFields.Contains(item.Key))
                    fieldsToFilter.Add(item.Key, item.Value);

            List<Expression<Func<T, bool>>> lst = new List<Expression<Func<T, bool>>>();

            foreach (var queryItem in fieldsToFilter)
            {
                query = query.Where(GetColumnEquality<T>(queryItem.Key, queryItem.Value));
            }

            return query;
        }


        private static Expression<Func<T, bool>> GetColumnEquality<T>(string property, string term)
        {
            #region .:: Stackoverflow ::.
            // https://stackoverflow.com/questions/17832989/linq-iqueryable-generic-filter/17833880#17833880
            var obj = Expression.Parameter(typeof(T), "obj");
            
            var objProperty = Expression.PropertyOrField(obj, property);
            var converted = Convert.ChangeType(term, objProperty.Type);
            var objEquality = Expression.Equal(objProperty, Expression.Constant(converted));

            var lambda = Expression.Lambda<Func<T, bool>>(objEquality, obj);

            return lambda;
#endregion
        }
    }
}
