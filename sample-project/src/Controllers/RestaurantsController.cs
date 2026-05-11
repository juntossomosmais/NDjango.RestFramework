using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Filters;
using NDjango.RestFramework.Helpers;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

/// <summary>
/// Demonstrates customizing <see cref="BaseController{TOrigin,TDestination,TPrimaryKey,TContext}.Query"/>
/// to eager-load navigation properties. The configured Query is what every action filters and
/// resolves through, so GETs see the included data, and PUT/PATCH/DELETE see scoped rows.
/// Mirrors the template's <c>PersonsController</c>: <c>Query = ctx.Persons.AsNoTracking();</c>.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class RestaurantsController : BaseController<RestaurantDto, Restaurant, int, AppDbContext>
{
    private readonly AppDbContext _db;

    public RestaurantsController(
        RestaurantSerializer serializer,
        AppDbContext db,
        ILogger<Restaurant> logger)
        : base(serializer, db, new ActionOptions { AllowBulkDelete = true }, logger)
    {
        _db = db;

        // .Include pulls Profile + MenuItems eagerly. Combined with the nested-field
        // entries in Restaurant.GetFields() ("RestaurantProfile:Website", ...), the
        // JsonTransform projects the included Profile fields into responses.
        Query = db.Restaurants
            .Include(r => r.Profile)
            .Include(r => r.MenuItems)
            .AsNoTracking();

        AllowedFields = new[] { nameof(Restaurant.Id), nameof(Restaurant.Name) };
        Filters.Add(new QueryStringFilter<Restaurant>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Restaurant>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Restaurant, int>());
    }

    /// <summary>
    /// Demonstrates <see cref="BaseController{TOrigin,TDestination,TPrimaryKey,TContext}.ValidateDestroyAsync"/>:
    /// state-predicate that runs after the entity is loaded and before
    /// <see cref="BaseController{TOrigin,TDestination,TPrimaryKey,TContext}.PerformDestroyAsync"/>.
    /// Populating <paramref name="errors"/> short-circuits the DELETE with a
    /// <c>400 ValidationErrors</c> response. Mirrors the README example
    /// "Store has open orders → cannot be deleted".
    ///
    /// <para>
    /// The bulk-delete path (<c>DELETE ?ids=</c>) bypasses this hook by design — it runs a
    /// single <c>ExecuteDeleteAsync</c> and skips per-row validation. That's why bulk delete
    /// is opt-in (<see cref="ActionOptions.AllowBulkDelete"/>); enable it only when the rule
    /// the bulk path skips is genuinely safe to skip.
    /// </para>
    /// </summary>
    protected override async Task ValidateDestroyAsync(
        Restaurant instance,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken)
    {
        var hasMenuItems = await _db.MenuItems.AsNoTracking()
            .AnyAsync(m => m.RestaurantId == instance.Id, cancellationToken);
        if (hasMenuItems)
            errors.GetOrAdd(ValidationErrors.NonFieldErrorsKey)
                .Add("Cannot delete a restaurant that still has menu items.");
    }
}
