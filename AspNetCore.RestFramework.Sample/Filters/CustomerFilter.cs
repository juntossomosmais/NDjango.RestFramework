using System.Linq;
using AspNetCore.RestFramework.Core.Filters;
using AspNetRestFramework.Sample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AspNetRestFramework.Sample.Filters;

public class CustomerFilter : Filter<CustomerDocument>
{
    public override IQueryable<CustomerDocument> AddFilter(IQueryable<CustomerDocument> query, HttpRequest request)
    {
        return query.Include(x => x.Customer);
    }
}