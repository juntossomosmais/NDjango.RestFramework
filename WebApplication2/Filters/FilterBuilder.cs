using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication2.Controllers;
using WebApplication2.Models;

namespace WebApplication2.Filters
{
    public class FilterBuilder<Tcontext, TEntity> : BaseFilter<Tcontext, TEntity> where Tcontext : DbContext
                                                                                  where TEntity : BaseEntity
    {
        public FilterBuilder(Tcontext context) : base(context)
        {
        }

        public IQueryable<TEntity> Build()
        {
            return base.DbSet;
        }
    }
}
