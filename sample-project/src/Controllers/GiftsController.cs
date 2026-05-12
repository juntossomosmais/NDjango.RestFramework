using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

[Route("api/[controller]")]
[ApiController]
public class GiftsController : BaseController<GiftDto, Gift, int, AppDbContext>
{
    public GiftsController(
        GiftSerializer serializer,
        AppDbContext db,
        ILogger<Gift> logger)
        : base(serializer, db, logger)
    {
        AllowedFields = new[] { nameof(Gift.Id), nameof(Gift.Name), nameof(Gift.IsWrapped) };
        Filters.Add(new QueryStringFilter<Gift>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Gift>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Gift, int>());
    }
}
