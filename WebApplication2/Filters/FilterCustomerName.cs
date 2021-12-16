using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using WebApplication2.Controllers;

namespace WebApplication2.Filters
{
    public class FilterCustomerName : BackendFilter
    {
        public override Expression<Func<TDestination, bool>> FilterQuerySet<TDestination>(HttpRequest request, Expression<Func<TDestination, bool>> filter)
        {
            return filter;
        }
    }
}
