using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;
using Xunit;

namespace NDjango.RestFramework.ExternalSurfaceTest;

/// <summary>
/// Compiles and runs against the same public surface a real downstream consumer sees —
/// no <c>InternalsVisibleTo</c>, no test infrastructure, no <c>BaseController</c>. If the
/// library accidentally hides a member needed by the headless examples in the README,
/// this project will fail to build.
/// </summary>
public class ExternalConsumerSurfaceTests
{
    private static AppDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RunValidationAsync_FromExternalAssembly_ShouldRunPerFieldAndCrossField()
    {
        // Arrange — exactly the shape the README's "Using Serializer outside an HTTP context"
        // example shows: build serializer from DbContext, construct a 2-arg ValidationContext
        // for Create, run validation, then CreateAsync.
        using var dbContext = NewInMemoryContext();
        var serializer = new ThingSerializer(dbContext);
        var dto = new ThingDto { Name = "Hello" };
        var errors = new Dictionary<string, List<string>>();
        var context = new ValidationContext<int>(SerializerOperation.Create, default);

        // Act
        var validated = await serializer.RunValidationAsync(dto, context, errors);
        var created = await serializer.CreateAsync(validated);

        // Assert
        Assert.Empty(errors);
        Assert.True(serializer.PerFieldRan);
        Assert.True(serializer.CrossFieldRan);
        Assert.Equal("Hello!", created.Name);
        var persisted = await dbContext.Things.AsNoTracking().FirstAsync(t => t.Id == created.Id);
        Assert.Equal("Hello!", persisted.Name);
    }

    [Fact]
    public async Task ThreeArgValidationContextCtor_FromExternalAssembly_ShouldBePublic()
    {
        // Arrange — the README's PATCH example calls the 3-arg ValidationContext ctor.
        // This test compiles only if that ctor is `public`. (It used to be `internal`.)
        using var dbContext = NewInMemoryContext();
        var existing = new Thing { Name = "Original" };
        dbContext.Things.Add(existing);
        await dbContext.SaveChangesAsync();

        var serializer = new ThingSerializer(dbContext);
        var partial = new PartialJsonObject<ThingDto>("{\"name\":\"Updated\"}");
        var errors = new Dictionary<string, List<string>>();
        var context = new ValidationContext<int>(
            SerializerOperation.PartialUpdate, existing.Id, partial);

        // Act
        await serializer.RunValidationAsync(partial.Instance, context, errors, partial);
        var updated = await serializer.PartialUpdateAsync(partial, existing.Id);

        // Assert
        Assert.Empty(errors);
        Assert.NotNull(updated);
        Assert.Equal("Updated!", updated!.Name);
    }

    [Fact]
    public async Task MapAndApplyToDestination_AreReachableForOverrideFromExternalAssembly()
    {
        // Arrange — proves the protected virtual seams are actually overridable from a
        // sibling assembly (not just from the test assembly that has InternalsVisibleTo).
        // OverridingThingSerializer below exercises both seams.
        using var dbContext = NewInMemoryContext();
        var existing = new Thing { Name = "Old" };
        dbContext.Things.Add(existing);
        await dbContext.SaveChangesAsync();

        var serializer = new OverridingThingSerializer(dbContext);

        // Act — Create exercises MapToDestination, Update exercises ApplyToDestination.
        var created = await serializer.CreateAsync(new ThingDto { Name = "Brand" });
        var updated = await serializer.UpdateAsync(new ThingDto { Name = "New" }, existing.Id);

        // Assert
        Assert.Equal("Brand_mapped", created.Name);
        Assert.NotNull(updated);
        Assert.Equal("New_applied", updated!.Name);
        Assert.Equal(existing.Id, updated.Id);
    }
}

// Minimal DTO/entity/DbContext defined here so the project does not depend on any
// shared test fixture from the main test project.

public class Thing : BaseModel<int>
{
    public string? Name { get; set; }

    public override string[] GetFields() => new[] { nameof(Id), nameof(Name) };
}

public class ThingDto : BaseDto<int>
{
    public string Name { get; set; } = string.Empty;
}

public class AppDbContext : DbContext
{
    public DbSet<Thing> Things { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Thing>(entity => { entity.HasKey(t => t.Id); });
    }
}

public class ThingSerializer : Serializer<ThingDto, Thing, int, AppDbContext>
{
    public bool PerFieldRan { get; private set; }
    public bool CrossFieldRan { get; private set; }

    public ThingSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    public Task<string> ValidateNameAsync(
        string value,
        ValidationContext<int> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        PerFieldRan = true;
        return Task.FromResult(value + "!");
    }

    public override Task<ThingDto> ValidateAsync(
        ThingDto data,
        ValidationContext<int> context,
        IDictionary<string, List<string>> errors,
        CancellationToken cancellationToken = default)
    {
        CrossFieldRan = true;
        return Task.FromResult(data);
    }
}

/// <summary>
/// Overrides both mapping seams. Compiles only if <c>MapToDestination</c> and
/// <c>ApplyToDestination</c> are <c>protected virtual</c> on the public surface.
/// </summary>
public class OverridingThingSerializer : Serializer<ThingDto, Thing, int, AppDbContext>
{
    public OverridingThingSerializer(AppDbContext applicationDbContext) : base(applicationDbContext)
    {
    }

    protected override Thing MapToDestination(ThingDto origin) =>
        new() { Name = origin.Name + "_mapped" };

    protected override void ApplyToDestination(ThingDto origin, Thing destination, int entityId)
    {
        destination.Id = entityId;
        destination.Name = origin.Name + "_applied";
    }
}
