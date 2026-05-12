using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;

namespace SampleProject.Serializers;

/// <summary>
/// Demonstrates per-field validation hooks. The library auto-discovers
/// <c>Validate{PropertyName}Async</c> methods on the serializer subclass and runs them
/// before <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.ValidateAsync"/>.
///
/// <para>
/// Each hook returns the (optionally normalized) value that ends up in the persisted entity —
/// DRF semantics: the returned value replaces the inbound value. Populate <c>errors</c> to
/// short-circuit the request with a <see cref="NDjango.RestFramework.Errors.ValidationErrors"/>
/// 400 envelope.
/// </para>
///
/// <para>
/// On PARTIAL UPDATE (PATCH), per-field hooks only fire for fields the payload actually
/// carried — the library threads the partial's <c>IsSet</c> probe through the pipeline
/// so omitted fields don't get re-validated against a defaulted value.
/// </para>
/// </summary>
public class TagSerializer : Serializer<TagDto, Tag, int, AppDbContext>
{
    private static readonly Regex NonAlphanumericRun = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex MultipleDashes = new("-{2,}", RegexOptions.Compiled);

    public TagSerializer(AppDbContext db) : base(db) { }

    /// <summary>
    /// Required, non-empty, max 100 chars, unique per database. Trims surrounding whitespace.
    /// The trimmed value is what gets persisted (and what the response echoes back).
    /// </summary>
    public async Task<string> ValidateNameAsync(
        string value,
        ValidationContext<int> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken)
    {
        var trimmed = (value ?? string.Empty).Trim();

        if (trimmed.Length == 0)
        {
            errors.GetOrAdd(nameof(TagDto.Name)).Add("Name is required.");
            return trimmed;
        }

        if (trimmed.Length > 100)
        {
            errors.GetOrAdd(nameof(TagDto.Name)).Add("Name must be at most 100 characters.");
            return trimmed;
        }

        var clashes = await _dbContext.Tags.AsNoTracking()
            .Where(t => t.Name == trimmed && t.Id != context.EntityId)
            .AnyAsync(cancellationToken);
        if (clashes)
            errors.GetOrAdd(nameof(TagDto.Name)).Add("A tag with this name already exists.");

        return trimmed;
    }

    /// <summary>
    /// Normalizes to lowercase kebab-case (alphanumeric runs joined by single dashes) and
    /// enforces uniqueness. Demonstrates the value-replacing return — the persisted slug is
    /// the normalized form, not the raw input.
    /// </summary>
    public async Task<string> ValidateSlugAsync(
        string value,
        ValidationContext<int> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken)
    {
        var raw = (value ?? string.Empty).Trim().ToLowerInvariant();
        var normalized = NonAlphanumericRun.Replace(raw, "-");
        normalized = MultipleDashes.Replace(normalized, "-").Trim('-');

        if (normalized.Length == 0)
        {
            errors.GetOrAdd(nameof(TagDto.Slug))
                .Add("Slug must contain at least one alphanumeric character.");
            return normalized;
        }

        var clashes = await _dbContext.Tags.AsNoTracking()
            .Where(t => t.Slug == normalized && t.Id != context.EntityId)
            .AnyAsync(cancellationToken);
        if (clashes)
            errors.GetOrAdd(nameof(TagDto.Slug)).Add("A tag with this slug already exists.");

        return normalized;
    }

    /// <summary>
    /// Cross-field rule: <c>ValidateAsync</c> runs <i>only after</i> every per-field hook
    /// has succeeded (no errors), so we can safely assume <c>Name</c> is already trimmed
    /// and <c>Slug</c> is already normalized. Mirrors the README example
    /// "Name cannot be the same as CNPJ".
    ///
    /// <para>
    /// On PATCH, only fields the payload actually carried have been re-run through their
    /// per-field hooks; <see cref="ValidationContext{T}.IsSet"/> tells us which. Skipping
    /// the rule when either field is absent matches the partial-update semantics — the
    /// caller hasn't expressed an opinion on the absent field, so we shouldn't reject
    /// based on a defaulted value.
    /// </para>
    /// </summary>
    public override Task<TagDto> ValidateAsync(
        TagDto data,
        ValidationContext<int> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        if (!context.IsSet(nameof(TagDto.Name)) || !context.IsSet(nameof(TagDto.Slug)))
            return Task.FromResult(data);

        var nameAsSlug = MultipleDashes.Replace(
            NonAlphanumericRun.Replace((data.Name ?? string.Empty).Trim().ToLowerInvariant(), "-"),
            "-").Trim('-');

        if (!string.IsNullOrEmpty(nameAsSlug) && nameAsSlug == data.Slug)
            errors.GetOrAdd(nameof(TagDto.Name))
                .Add("Name and Slug must differ once normalized.");

        return Task.FromResult(data);
    }
}
