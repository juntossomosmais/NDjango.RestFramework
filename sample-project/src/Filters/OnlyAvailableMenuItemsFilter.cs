using Microsoft.AspNetCore.Http;
using NDjango.RestFramework.Filters;

namespace SampleProject.Filters;

/// <summary>
/// Custom domain filter. Activates only when the request carries
/// <c>?onlyAvailable=true</c>; otherwise it returns the queryset untouched so the existing
/// list-all behavior keeps working.
///
/// <para>
/// Mirrors the README's <c>ActiveOnlyFilter</c> / <c>TodoItemIncludePersonFilter</c> shape:
/// a <see cref="Filter{TEntity}"/> subclass is the right seam whenever an LIST endpoint
/// needs an EF Core operation that goes beyond <c>QueryStringFilter</c>'s equality match.
/// </para>
/// </summary>
public class OnlyAvailableMenuItemsFilter : Filter<MenuItem>
{
    public override IQueryable<MenuItem> AddFilter(IQueryable<MenuItem> query, HttpRequest request)
    {
        if (!request.Query.TryGetValue("onlyAvailable", out var raw))
            return query;

        if (!bool.TryParse(raw, out var only) || !only)
            return query;

        return query.Where(m => m.IsAvailable);
    }
}
