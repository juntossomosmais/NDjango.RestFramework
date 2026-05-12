using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

[Route("api/[controller]")]
[ApiController]
public class IngredientsController : BaseController<IngredientDto, Ingredient, int, AppDbContext>
{
    public IngredientsController(
        IngredientSerializer serializer,
        AppDbContext db,
        ILogger<Ingredient> logger)
        : base(serializer, db, logger)
    {
        AllowedFields = new[] { nameof(Ingredient.Id), nameof(Ingredient.Name), nameof(Ingredient.IsAllergen) };
        Filters.Add(new QueryStringFilter<Ingredient>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Ingredient>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Ingredient, int>());
    }
}
