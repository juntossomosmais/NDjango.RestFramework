using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

/// <summary>
/// Exercises <see cref="ActionOptions.AllowPut"/> <i>in isolation</i>: only the full-replace
/// endpoint is gated, so a client that issues <c>PUT /api/Gifts/{id}</c> gets <c>405</c> while
/// POST / GET / PATCH / DELETE remain open. Pairs with the existing <c>MenuItems</c> coverage
/// (AllowDelete=false alone) and <c>AuditLogs</c> coverage (every mutating flag off together)
/// to pin each flag as an independent gate.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class GiftsController : BaseController<GiftDto, Gift, int, AppDbContext>
{
    public GiftsController(
        GiftSerializer serializer,
        AppDbContext db,
        ILogger<Gift> logger)
        : base(serializer, db, new ActionOptions { AllowPut = false }, logger)
    {
        AllowedFields = new[] { nameof(Gift.Id), nameof(Gift.Name), nameof(Gift.IsWrapped) };
        Filters.Add(new QueryStringFilter<Gift>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Gift>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Gift, int>());
    }
}
