using Microsoft.AspNetCore.Http;
using NDjango.RestFramework.Filters;

namespace SampleProject.Filters;

/// <summary>
/// Tenant-scoping filter — mirrors the README's "Queryset scope on writes" example.
/// Composes into the load step of every list and write action on <c>TenantNotesController</c>,
/// so out-of-scope ids surface as 404 on GET single / PUT / PATCH / DELETE and silently drop
/// out of bulk DELETE.
///
/// <para>
/// Defensive empty-set when the <c>X-Tenant</c> header is absent or blank — the README warns
/// that a tenant filter which silently falls back to "all rows" defeats the purpose.
/// </para>
/// </summary>
public class TenantNoteTenantFilter : Filter<TenantNote>
{
    public const string TenantHeaderName = "X-Tenant";

    public override IQueryable<TenantNote> AddFilter(IQueryable<TenantNote> query, HttpRequest request)
    {
        if (!request.Headers.TryGetValue(TenantHeaderName, out var values))
            return query.Where(_ => false);

        var tenant = values.ToString();
        if (string.IsNullOrWhiteSpace(tenant))
            return query.Where(_ => false);

        return query.Where(n => n.TenantId == tenant);
    }
}
