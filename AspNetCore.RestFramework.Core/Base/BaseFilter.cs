using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.RestFramework.Core.Base;

public class BaseFilter<TContext, TEntity>
    where TContext : DbContext
    where TEntity : class
{
    public IQueryable<TEntity> DbSet { get; set; }

    public BaseFilter(TContext context)
    {
        DbSet = context.Set<TEntity>();
    }
}