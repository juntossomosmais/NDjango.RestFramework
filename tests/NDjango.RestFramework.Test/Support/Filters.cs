using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Filters;

namespace NDjango.RestFramework.Test.Support;

public class CustomerDocumentIncludeFilter : Filter<Customer>
{
    public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
    {
        return query.Include(x => x.CustomerDocument);
    }
}

/// <summary>
/// Row-scoping filter used by the cross-tenant write security tests. Reads the tenant
/// identifier from an <c>X-Tenant</c> header and restricts the queryset to rows whose
/// <see cref="Customer.Region"/> matches. When the header is absent the queryset is
/// emptied — a defensive default that mirrors how a production tenant filter would
/// refuse to fall back to "all rows".
/// </summary>
public class TenantFilter : Filter<Customer>
{
    private const string TenantHeader = "X-Tenant";

    public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
    {
        if (!request.Headers.TryGetValue(TenantHeader, out var values) || string.IsNullOrWhiteSpace(values.ToString()))
            return query.Where(_ => false);

        var tenant = values.ToString();
        return query.Where(c => c.Region == tenant);
    }
}

public class CustomerFilter : Filter<CustomerDocument>
{
    public override IQueryable<CustomerDocument> AddFilter(IQueryable<CustomerDocument> query, HttpRequest request)
    {
        return query.Include(x => x.Customer);
    }
}

public class DocumentFilter : Filter<Customer>
{
    public override IQueryable<Customer> AddFilter(IQueryable<Customer> query, HttpRequest request)
    {
        var queryString = request.Query.Select(x => new { x.Key, x.Value }).ToList();

        var documentType = "";

        if (queryString.Any(x => x.Key == "cpf"))
            documentType = "cpf";

        if (queryString.Any(x => x.Key == "cnpj"))
            documentType = "cnpj";

        var document = queryString.FirstOrDefault(x => x.Key == documentType)?.Value;
        if (string.IsNullOrWhiteSpace(document))
            return query;


        return query.Where(x => x.CustomerDocument.Any(x => x.DocumentType == documentType && x.Document == document.Value.ToString()));
    }
}
