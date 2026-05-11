using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RestaurantProfilesController : BaseController<RestaurantProfileDto, RestaurantProfile, int, AppDbContext>
{
    public RestaurantProfilesController(
        RestaurantProfileSerializer serializer,
        AppDbContext db,
        ILogger<RestaurantProfile> logger)
        : base(serializer, db, logger)
    {
        AllowedFields = new[] { nameof(RestaurantProfile.Id), nameof(RestaurantProfile.RestaurantId) };
        Filters.Add(new QueryStringFilter<RestaurantProfile>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<RestaurantProfile, int>());
    }
}
