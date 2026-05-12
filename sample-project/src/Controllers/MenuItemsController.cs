using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

/// <summary>
/// Exercises <see cref="ActionOptions.AllowDelete"/>: single-resource DELETE is disabled so a
/// client that issues <c>DELETE /api/MenuItems/{id}</c> gets <c>405 Method Not Allowed</c> while
/// the rest of the CRUD surface stays open. The endpoint stays listed in OpenAPI (inline 405,
/// not [NonAction]) — documented but off by default.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class MenuItemsController : BaseController<MenuItemDto, MenuItem, int, AppDbContext>
{
    public MenuItemsController(
        MenuItemSerializer serializer,
        AppDbContext db,
        ILogger<MenuItem> logger)
        : base(serializer, db, new ActionOptions { AllowDelete = false }, logger)
    {
        AllowedFields = new[]
        {
            nameof(MenuItem.Id),
            nameof(MenuItem.RestaurantId),
            nameof(MenuItem.Name),
            nameof(MenuItem.IsAvailable),
        };
        Filters.Add(new QueryStringFilter<MenuItem>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<MenuItem>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<MenuItem, int>());
        // Domain filter — activates only when ?onlyAvailable=true is on the request.
        Filters.Add(new OnlyAvailableMenuItemsFilter());
    }
}
