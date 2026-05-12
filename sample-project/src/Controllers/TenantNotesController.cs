using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

/// <summary>
/// Demonstrates two README contracts in one place:
///
/// <list type="bullet">
///   <item>
///     <b>Queryset scope on writes</b> — the <see cref="TenantNoteTenantFilter"/> composes
///     into the load step of every action, so out-of-scope ids surface as 404 on GET/{id},
///     PUT/{id}, PATCH/{id}, DELETE/{id}, and silently drop from <c>DELETE ?ids=</c>.
///     <c>AllowBulkDelete = true</c> so the bulk path is exercised end-to-end.
///   </item>
///   <item>
///     <b><c>PerformCreateAsync</c> override</b> — request-shaped side effect: stamps the
///     <c>TenantId</c> on the DTO from the <c>X-Tenant</c> header before delegating to the
///     serializer, so the header (not the body) is the canonical source of truth.
///     Matches the README example pattern.
///   </item>
/// </list>
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TenantNotesController : BaseController<TenantNoteDto, TenantNote, int, AppDbContext>
{
    public TenantNotesController(
        TenantNoteSerializer serializer,
        AppDbContext db,
        ILogger<TenantNote> logger)
        : base(serializer, db, new ActionOptions { AllowBulkDelete = true }, logger)
    {
        AllowedFields = new[]
        {
            nameof(TenantNote.Id),
            nameof(TenantNote.TenantId),
            nameof(TenantNote.Title),
        };
        Filters.Add(new QueryStringFilter<TenantNote>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<TenantNote, int>());
        // Row-scoping filter — composes with the rest of the chain on every action.
        Filters.Add(new TenantNoteTenantFilter());
    }

    /// <summary>
    /// Stamps <c>TenantId</c> from the <c>X-Tenant</c> header onto the DTO before the
    /// serializer's create runs. Any body-supplied <c>TenantId</c> is overwritten — the
    /// header is the canonical source of truth, and POST is not gated by <c>Filters</c> on
    /// <see cref="BaseController{TOrigin,TDestination,TPrimaryKey,TContext}.Post"/>.
    /// </summary>
    protected override Task<TenantNote> PerformCreateAsync(
        TenantNoteDto data,
        CancellationToken cancellationToken)
    {
        if (Request.Headers.TryGetValue(TenantNoteTenantFilter.TenantHeaderName, out var values))
            data.TenantId = values.ToString();

        return base.PerformCreateAsync(data, cancellationToken);
    }
}
