using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AspNetCore.RestFramework.Core.Filters
{
    public class QueryStringSearchFilter<TEntity> : Filter<TEntity>
    {
        private readonly string[] _allowedFields;

        public QueryStringSearchFilter(string[] allowedFields)
        {
            _allowedFields = allowedFields;
        }

        public override IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request)
        {
            var searchFilter = request.Query.FirstOrDefault(m => m.Key.Equals("search", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrWhiteSpace(searchFilter.Value))
                return query;

            var parameter = Expression.Parameter(typeof(TEntity), "obj");
            var expressions = new List<Expression>();
            
            foreach (var field in _allowedFields)
            {
                if (TryGetColumnFilter(field, searchFilter.Value, parameter, out Expression expression))
                    expressions.Add(expression);
            }

            return FilterAllExpressions(query, parameter, expressions);
        }

        private static IQueryable<TEntity> FilterAllExpressions(IQueryable<TEntity> query, ParameterExpression parameter, List<Expression> expressions)
        {
            if (expressions.Count > 0)
            {
                var firstFilter = expressions[0];
                var filterExpression = Expression.Lambda<Func<TEntity, bool>>(firstFilter, parameter);

                if (expressions.Count > 1)
                {
                    Expression outerExpression = expressions[0];
                    for (int i = 1; i < expressions.Count; ++i)
                    {
                        var aux = outerExpression;
                        outerExpression = Expression.Or(aux, expressions[i]);
                    }

                    filterExpression = Expression.Lambda<Func<TEntity, bool>>(outerExpression, parameter);
                }

                query = query.Where(filterExpression);
            }

            return query;
        }

        private static bool TryGetColumnFilter(string property, string term, ParameterExpression parameter, out Expression expression)
        {
            var objProperty = Expression.PropertyOrField(parameter, property);

            if (objProperty.Type.IsAssignableTo(typeof(Guid)) && Guid.TryParse(term, out Guid id))
            {
                expression = Expression.Equal(objProperty, Expression.Constant(id));
            }
            else if (objProperty.Type.IsAssignableTo(typeof(string)))
            {
                var efLikeMethod = typeof(DbFunctionsExtensions).GetMethod(
                    nameof(DbFunctionsExtensions.Like),
                    BindingFlags.Public | BindingFlags.Static,
                    new[] { typeof(DbFunctions), typeof(string), typeof(string) }
                );
                expression = Expression.Call(efLikeMethod, Expression.Constant(EF.Functions), objProperty, Expression.Constant(term));
            }
            else if (TryConvertValue(term, objProperty.Type, out object convertedValue))
            {
                expression = Expression.Equal(objProperty, Expression.Constant(convertedValue));
            }
            else
            {
                expression = null;
                return false;
            }

            return true;
        }

        private static bool TryConvertValue(string value, Type conversionType, out object result)
        {
            try
            {
                result = Convert.ChangeType(value, conversionType);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
