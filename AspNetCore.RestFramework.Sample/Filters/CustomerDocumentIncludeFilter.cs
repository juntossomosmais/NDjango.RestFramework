using System.Linq;
using AspNetCore.RestFramework.Core.Filters;
using AspNetRestFramework.Sample.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace AspNetRestFramework.Sample.Filters;

public class CustomerDocumentIncludeFilter : Filter<Customer>
{
    public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
    {
        return query.Include(x => x.CustomerDocument);
    }
}