using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CSharpRestFramework.Filters
{
    public class SortFilter<TEntity>
    {
        public IQueryable<TEntity> Sort(IQueryable<TEntity> query, HttpRequest httpRequest, string[] allowedFilters)
        {
            if (httpRequest.Query.ContainsKey("Sort"))
                return SortAsc(query, httpRequest, allowedFilters);
            else if(httpRequest.Query.ContainsKey("SortDesc"))
                return SortDesc(query, httpRequest, allowedFilters);

            return query;
        }

        private IQueryable<TEntity> SortAsc(IQueryable<TEntity> query, HttpRequest httpRequest, string[] allowedFilters)
        {
            var sortElements = httpRequest.Query.First(x => x.Key == "Sort").Value.ToString().Split(",");
            var filtered = sortElements.Where(x => allowedFilters.Contains(x)).ToArray();

            if (!filtered.Any())
                return query;

            query = OrderBy<TEntity>(query, filtered.First());

            foreach (var item in filtered.Skip(1))
                query = ThenBy<TEntity>(query, item);

            return query;
        }

        private IQueryable<TEntity> SortDesc(IQueryable<TEntity> query, HttpRequest httpRequest, string[] allowedFilters)
        {
            var sortElements = httpRequest.Query.First(x => x.Key == "SortDesc").Value.ToString().Split(",");
            var filtered = sortElements.Where(x => allowedFilters.Contains(x)).ToArray();

            if (!filtered.Any())
                return query;

            query = OrderByDescending<TEntity>(query, filtered.First());

            foreach (var item in filtered.Skip(1))
                query = ThenByDescending<TEntity>(query, item);

            return query;
        }

        private IQueryable<TEntity> OrderBy<IQueryable>(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "OrderBy");

        private IQueryable<TEntity> ThenBy<IQueryable>(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "ThenBy");

        private IQueryable<TEntity> OrderByDescending<IQueryable>(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "OrderByDescending");

        private IQueryable<TEntity> ThenByDescending<IQueryable>(IQueryable<TEntity> query, string orderByProperty) =>
            Order(query, orderByProperty, "ThenByDescending");

        private IQueryable<TEntity> Order(IQueryable<TEntity> query, string orderByProperty, string operation)
        {
            #region .:: Stackoverflow ::.
            // https://stackoverflow.com/questions/7265186/how-do-i-specify-the-linq-orderby-argument-dynamically
            var type = typeof(TEntity);
            var objProperty = type.GetProperty(orderByProperty);
            var parameter = Expression.Parameter(type, "param");
            var propertyAccess = Expression.MakeMemberAccess(parameter, objProperty);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), operation, new Type[] { type, objProperty.PropertyType },
                                          query.Expression, Expression.Quote(orderByExpression));
            return query.Provider.CreateQuery<TEntity>(resultExpression);
            #endregion
        }

    }
}
