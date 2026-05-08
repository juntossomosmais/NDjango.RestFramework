using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Test.Support;
using Xunit;

namespace NDjango.RestFramework.Test.Serializer;

/// <summary>
/// Verifies the <see cref="NDjango.RestFramework.Serializer.Serializer{T1,T2,T3,T4}.MapToDestination"/>
/// and <see cref="NDjango.RestFramework.Serializer.Serializer{T1,T2,T3,T4}.ApplyToDestination"/>
/// extension seams. Two angles: (1) the default Newtonsoft round-trip behavior is preserved
/// when subclasses do not override; (2) when subclasses do override, the override runs
/// instead of the default and receives the right arguments.
/// </summary>
public class MappingSeamTests
{
    private static AppDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public class CreateAsyncMapping
    {
        [Fact]
        public async Task CreateAsync_DefaultSerializer_ShouldMapDtoFieldsViaNewtonsoftRoundTrip()
        {
            // Arrange — CustomerSerializer does not override MapToDestination, so the
            // default Newtonsoft round-trip should produce a Customer with matching fields.
            using var dbContext = NewInMemoryContext();
            var serializer = new CustomerSerializer(dbContext);
            var data = new CustomerDto { Name = "Plain", CNPJ = "12345678000190" };

            // Act
            var created = await serializer.CreateAsync(data);

            // Assert
            Assert.Equal("Plain", created.Name);
            Assert.Equal("12345678000190", created.CNPJ);
            Assert.NotEqual(Guid.Empty, created.Id);
        }

        [Fact]
        public async Task CreateAsync_OverriddenMapToDestination_ShouldRunOverrideInsteadOfDefault()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var serializer = new MappingSeamSpySerializer(dbContext);
            var data = new CustomerDto { Name = "Plain", CNPJ = "12345678000190" };

            // Act
            var created = await serializer.CreateAsync(data);

            // Assert
            Assert.True(serializer.MapToDestinationCalled,
                "Overridden MapToDestination should be invoked by CreateAsync.");
            Assert.Equal("Plain_mapped", created.Name);
            var persisted = await dbContext.Customer.AsNoTracking()
                .FirstAsync(c => c.Id == created.Id);
            Assert.Equal("Plain_mapped", persisted.Name);
        }
    }

    public class UpdateAsyncMapping
    {
        [Fact]
        public async Task UpdateAsync_DefaultApplyToDestination_ShouldPreserveEntityIdEvenWhenDtoCarriesDifferentId()
        {
            // Arrange — DTO carries a rogue Id that must not overwrite the real PK
            // because the default ApplyToDestination forces destination.Id = entityId.
            using var dbContext = NewInMemoryContext();
            var realId = Guid.NewGuid();
            dbContext.Customer.Add(new Customer { Id = realId, Name = "Original", CNPJ = "old" });
            await dbContext.SaveChangesAsync();

            var serializer = new CustomerSerializer(dbContext);
            var data = new CustomerDto
            {
                Id = Guid.NewGuid(),
                Name = "Updated",
                CNPJ = "new",
            };

            // Act — headless caller loads the instance first (DRF parity, mixins.py:58-67 at tag 3.17.1).
            var instance = await dbContext.Customer.FirstAsync(c => c.Id == realId);
            var updated = await serializer.UpdateAsync(instance, data);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal(realId, updated.Id);
            Assert.Equal("Updated", updated.Name);
            Assert.Equal("new", updated.CNPJ);
        }

        [Fact]
        public async Task UpdateAsync_OverriddenApplyToDestination_ShouldReceiveTheTargetEntityId()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var realId = Guid.NewGuid();
            dbContext.Customer.Add(new Customer { Id = realId, Name = "Original", CNPJ = "old" });
            await dbContext.SaveChangesAsync();

            var serializer = new MappingSeamSpySerializer(dbContext);
            var data = new CustomerDto { Name = "Updated", CNPJ = "new" };

            // Act — headless caller loads the instance first (DRF parity, mixins.py:58-67 at tag 3.17.1).
            var instance = await dbContext.Customer.FirstAsync(c => c.Id == realId);
            var updated = await serializer.UpdateAsync(instance, data);

            // Assert
            Assert.NotNull(updated);
            Assert.True(serializer.ApplyToDestinationCalled,
                "Overridden ApplyToDestination should be invoked by UpdateAsync.");
            Assert.Single(serializer.ApplyToDestinationEntityIds);
            Assert.Equal(realId, serializer.ApplyToDestinationEntityIds[0]);
            Assert.Equal("Updated_applied", updated.Name);
        }
    }

    public class UpdateManyAsyncMapping
    {
        [Fact]
        public async Task UpdateManyAsync_OverriddenApplyToDestination_ShouldBeCalledOncePerEntityWithMatchingId()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
            foreach (var id in ids)
                dbContext.Customer.Add(new Customer { Id = id, Name = "Original", CNPJ = "old" });
            await dbContext.SaveChangesAsync();

            var serializer = new MappingSeamSpySerializer(dbContext);
            var data = new CustomerDto { Name = "Bulk", CNPJ = "new" };

            // Act
            await serializer.UpdateManyAsync(dbContext.Customer, data, ids);

            // Assert
            Assert.Equal(ids.Length, serializer.ApplyToDestinationEntityIds.Count);
            Assert.Equal(
                ids.OrderBy(x => x).ToArray(),
                serializer.ApplyToDestinationEntityIds.OrderBy(x => x).ToArray());
        }
    }

    public class PartialUpdateBypassesSeams
    {
        [Fact]
        public async Task PartialUpdateAsync_ShouldNotInvokeApplyToDestination()
        {
            // Arrange — PartialUpdateAsync walks PartialJsonObject.IsSet directly and copies
            // matching properties via reflection; it intentionally does not route through the
            // ApplyToDestination seam. This test pins that behavior so a future refactor that
            // wires PATCH through the seam fails loudly instead of silently changing the
            // mapping contract.
            using var dbContext = NewInMemoryContext();
            var realId = Guid.NewGuid();
            dbContext.Customer.Add(new Customer { Id = realId, Name = "Original", CNPJ = "old" });
            await dbContext.SaveChangesAsync();

            var serializer = new MappingSeamSpySerializer(dbContext);
            var partial = new PartialJsonObject<CustomerDto>("{\"name\":\"NewName\"}");

            // Act — headless caller loads the instance first (DRF parity, mixins.py:58-67 at tag 3.17.1).
            var instance = await dbContext.Customer.FirstAsync(c => c.Id == realId);
            var updated = await serializer.PartialUpdateAsync(instance, partial);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("NewName", updated.Name);
            Assert.False(serializer.ApplyToDestinationCalled,
                "PartialUpdateAsync must not invoke the ApplyToDestination seam.");
        }

        [Fact]
        public async Task PartialUpdateAsync_OriginCarriesPropertyAbsentOnDestination_ShouldSkipSilentlyAndApplyMatchingFields()
        {
            // Arrange — TOrigin (CustomerWithUnknownFieldDto) declares SecretSauce, but the
            // destination (Customer) has no such property. PartialUpdateAsync reflects over
            // TOrigin's properties; the destination lookup for SecretSauce returns null.
            // DRF's ModelSerializer.update silently skips unknown fields — this test pins
            // that parity so a future "fail loud" refactor (or the prior NRE-throwing code)
            // breaks loudly here instead of in production.
            using var dbContext = NewInMemoryContext();
            var realId = Guid.NewGuid();
            dbContext.Customer.Add(new Customer { Id = realId, Name = "Original", CNPJ = "old" });
            await dbContext.SaveChangesAsync();

            var serializer = new CustomerWithUnknownFieldSerializer(dbContext);
            var partial = new PartialJsonObject<CustomerWithUnknownFieldDto>(
                "{\"name\":\"Renamed\",\"secretSauce\":\"42\"}");

            // Act
            var instance = await dbContext.Customer.FirstAsync(c => c.Id == realId);
            var updated = await serializer.PartialUpdateAsync(instance, partial);

            // Assert — matching property landed, unknown property was skipped without throwing.
            Assert.NotNull(updated);
            Assert.Equal("Renamed", updated.Name);
            Assert.Equal("old", updated.CNPJ);
            var persisted = await dbContext.Customer.AsNoTracking().FirstAsync(c => c.Id == realId);
            Assert.Equal("Renamed", persisted.Name);
        }
    }

    public class OverrideOwnsAllCopying
    {
        [Fact]
        public async Task CreateAsync_OverrideSkipsAField_ShouldNotFallBackToDefaultRoundTrip()
        {
            // Arrange — pins the documented contract that overriding MapToDestination fully
            // replaces the default Newtonsoft round-trip. PartialMappingSerializer copies
            // only Name and intentionally omits CNPJ; if the framework silently fell back
            // to the default for skipped fields, CNPJ would be populated.
            using var dbContext = NewInMemoryContext();
            var serializer = new PartialMappingSerializer(dbContext);
            var data = new CustomerDto { Name = "Kept", CNPJ = "12345678000190" };

            // Act
            var created = await serializer.CreateAsync(data);

            // Assert
            Assert.Equal("Kept", created.Name);
            Assert.Null(created.CNPJ);
        }
    }
}
