using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace AspNetCore.RestFramework.Core.Filters
{
    public class SortFilter<TEntity>
    {
        public IQueryable<TEntity> Sort(IQueryable<TEntity> query, HttpRequest httpRequest, string[] allowedFilters)
        {
            if (httpRequest.Query.Keys.Any(k => k.Equals("Sort", StringComparison.OrdinalIgnoreCase)))
                return SortAsc(query, httpRequest, allowedFilters);
            else if (httpRequest.Query.Keys.Any(k => k.Equals("SortDesc", StringComparison.OrdinalIgnoreCase)))
                return SortDesc(query, httpRequest, allowedFilters);

            return SortById(query);
        }

        private IQueryable<TEntity> SortAsc(IQueryable<TEntity> query, HttpRequest httpRequest, string[] allowedFilters)
        {
            var parameterValue = httpRequest.Query.First(x => x.Key.Equals("Sort", StringComparison.OrdinalIgnoreCase)).Value;
            var sortElements = parameterValue.ToString().Split(",");
            var filtered = sortElements.Where(x => allowedFilters.Any(f => f.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();

            if (!filtered.Any())
                return query;

            query = OrderBy(query, filtered.First());

            foreach (var item in filtered.Skip(1))
                query = ThenBy(query, item);

            return query;
        }

        private IQueryable<TEntity> SortDesc(IQueryable<TEntity> query, HttpRequest httpRequest, string[] allowedFilters)
        {
            var parameterValue = httpRequest.Query.First(x => x.Key.Equals("SortDesc", StringComparison.OrdinalIgnoreCase)).Value;
            var sortElements = parameterValue.ToString().Split(",");
            var filtered = sortElements.Where(x => allowedFilters.Any(f => f.Equals(x, StringComparison.OrdinalIgnoreCase))).ToArray();

            if (!filtered.Any())
                return query;

            query = OrderByDescending(query, filtered.First());

            foreach (var item in filtered.Skip(1))
                query = ThenByDescending(query, item);

            return query;
        }

        private IQueryable<TEntity> SortById(IQueryable<TEntity> query)
        {
            query = OrderBy(query, "Id");

            query = ThenBy(query, "Id");

            return query;
        }

        private static IQueryable<TEntity> OrderBy(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "OrderBy");

        private static IQueryable<TEntity> ThenBy(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "ThenBy");

        private static IQueryable<TEntity> OrderByDescending(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "OrderByDescending");

        private static IQueryable<TEntity> ThenByDescending(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "ThenByDescending");

        private static IQueryable<TEntity> Order(IQueryable<TEntity> query, string orderByProperty, string operation)
        {
            /*
             * Modified the following solution to be case insensitive:
             * https://stackoverflow.com/questions/7265186/how-do-i-specify-the-linq-orderby-argument-dynamically
             */

            var type = typeof(TEntity);
            var objProperty = type.GetProperties().First(p => p.Name.Equals(orderByProperty, StringComparison.OrdinalIgnoreCase));
            var parameter = Expression.Parameter(type, "param");
            var propertyAccess = Expression.MakeMemberAccess(parameter, objProperty);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), operation, new Type[] { type, objProperty.PropertyType },
                                          query.Expression, Expression.Quote(orderByExpression));
            return query.Provider.CreateQuery<TEntity>(resultExpression);
        }

    }
}
