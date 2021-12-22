using CSharpRestFramework.Base;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CSharpRestFramework.Filters
{
    public class FilterBuilder<Tcontext, TEntity> : BaseFilter<Tcontext, TEntity> where Tcontext : DbContext
                                                                                  where TEntity : class
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
