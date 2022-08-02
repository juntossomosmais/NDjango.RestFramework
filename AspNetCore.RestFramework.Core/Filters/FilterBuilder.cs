using Microsoft.EntityFrameworkCore;
using System.Linq;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetCore.RestFramework.Core.Filters
{
    public class FilterBuilder<TContext, TEntity> : BaseFilter<TContext, TEntity>
        where TContext : DbContext
        where TEntity : class
    {
        public FilterBuilder(TContext context) : base(context)
        {
        }

        public IQueryable<TEntity> Build()
        {
            return base.DbSet;
        }
    }
}
