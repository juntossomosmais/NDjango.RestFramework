using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Filters;
using SampleProject.Serializers;

namespace SampleProject.Controllers;

/// <summary>
/// Drives the per-field <c>Validate{Field}Async</c> seams on <see cref="TagSerializer"/>:
/// <c>ValidateNameAsync</c> rejects empty / duplicate names; <c>ValidateSlugAsync</c>
/// normalizes the slug to lowercase kebab-case and enforces uniqueness.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class TagsController : BaseController<TagDto, Tag, int, AppDbContext>
{
    public TagsController(
        TagSerializer serializer,
        AppDbContext db,
        ILogger<Tag> logger)
        : base(serializer, db, logger)
    {
        AllowedFields = new[] { nameof(Tag.Id), nameof(Tag.Name), nameof(Tag.Slug) };
        Filters.Add(new QueryStringFilter<Tag>(AllowedFields));
        Filters.Add(new QueryStringSearchFilter<Tag>(AllowedFields));
        Filters.Add(new QueryStringIdRangeFilter<Tag, int>());
    }
}
