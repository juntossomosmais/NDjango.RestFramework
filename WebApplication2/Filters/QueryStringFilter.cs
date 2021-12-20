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
    public class QueryStringFilter<Tcontext>  : Filter<Customer>
    {
        public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
        {
            return query;
        }
    }
}
