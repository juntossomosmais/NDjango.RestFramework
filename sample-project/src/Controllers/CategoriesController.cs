using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoriesController : BaseController<CategoryDto, Category, int, AppDbContext>
{
    public CategoriesController(
        CategorySerializer serializer,
        AppDbContext db,
        ILogger<Category> logger)
        : base(serializer, db, logger)
    {
        AllowedFields = new[] { nameof(Category.Id), nameof(Category.Name) };
        Filters.Add(new QueryStringFilter<Category>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Category>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Category, int>());
    }
}
