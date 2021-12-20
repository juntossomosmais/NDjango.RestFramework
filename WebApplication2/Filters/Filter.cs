using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication2.Filters
{
    public abstract class Filter<TEntity>
    {
        public abstract IQueryable<TEntity> AddFilter(IQueryable<TEntity> query, HttpRequest request);
    }
}
