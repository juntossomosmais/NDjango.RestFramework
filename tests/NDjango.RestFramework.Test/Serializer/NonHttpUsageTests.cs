using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Helpers;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using Xunit;

namespace NDjango.RestFramework.Test.Serializer;

/// <summary>
/// Exercises the serializer's public surface from a non-HTTP caller's perspective —
/// no <c>WebApplicationFactory</c>, no controllers, no model binding. These tests
/// guard the contract that lets message consumers, gRPC services, and scheduled jobs
/// reuse <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.RunValidationAsync"/>
/// and the CRUD methods directly.
/// </summary>
public class NonHttpUsageTests
{
    private static AppDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    public class RunValidationAsyncDirect
    {
        [Fact]
        public async Task RunValidationAsync_Create_PerFieldAndCrossFieldRun_ShouldNormalizeAndValidate()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var serializer = new PerFieldWithCrossFieldSerializer(dbContext);
            var data = new CustomerDto { Name = "ValidName", CNPJ = "12.345.678/0001-90" };
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Act
            var result = await serializer.RunValidationAsync(data, context, errors);

            // Assert
            Assert.Empty(errors);
            Assert.Equal("12345678000190", result.CNPJ);
            Assert.True(serializer.CrossFieldCalled,
                "Cross-field hook should run when per-field hooks pass.");
        }

        [Fact]
        public async Task RunValidationAsync_BulkUpdate_PerFieldHookSeesBulkUpdateContext()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var serializer = new ContextCapturingSerializer(dbContext);
            var data = new CustomerDto { Name = "ValidName", CNPJ = "12345678000190" };
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(SerializerOperation.BulkUpdate, default);

            // Act
            await serializer.RunValidationAsync(data, context, errors);

            // Assert
            Assert.NotNull(serializer.LastContext);
            Assert.True(serializer.LastContext.IsBulkUpdate,
                "Per-field hook should observe the BulkUpdate operation passed in.");
        }

        [Fact]
        public async Task RunValidationAsync_PartialUpdate_AbsentFieldsSkipPerFieldHooks()
        {
            // Arrange — only Name is sent; CNPJ hook must not run.
            using var dbContext = NewInMemoryContext();
            var serializer = new PerFieldShortCircuitSerializer(dbContext);
            var partial = new PartialJsonObject<CustomerDto>("{\"name\":\"OnlyName\"}");
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(
                SerializerOperation.PartialUpdate, Guid.NewGuid(), partial);

            // Act
            await serializer.RunValidationAsync(partial.Instance, context, errors, partial);

            // Assert
            Assert.False(errors.ContainsKey("CNPJ"),
                "CNPJ hook should be skipped when CNPJ is absent from the partial payload.");
        }

        [Fact]
        public async Task RunValidationAsync_PartialUpdate_HookMutationSyncsBackIntoPartialJson()
        {
            // Arrange — CNPJ is sent in formatted form; the per-field hook normalizes it,
            // and the framework should write the normalized value back into the partial JSON
            // so a downstream PartialUpdateAsync sees the digits-only form.
            using var dbContext = NewInMemoryContext();
            var serializer = new PerFieldCustomerSerializer(dbContext);
            var partial = new PartialJsonObject<CustomerDto>("{\"cnpj\":\"12.345.678/0001-90\"}");
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(
                SerializerOperation.PartialUpdate, Guid.NewGuid(), partial);

            // Act
            await serializer.RunValidationAsync(partial.Instance, context, errors, partial);

            // Assert
            Assert.Empty(errors);
            Assert.Equal("12345678000190", partial.Instance.CNPJ);
            Assert.True(partial.IsSet(nameof(CustomerDto.CNPJ)),
                "CNPJ should still be marked as set after the hook write-back.");
        }
    }

    public class HeadlessCreateAsync
    {
        [Fact]
        public async Task CreateAsync_HeadlessCallerWithoutController_ShouldPersistEntity()
        {
            // Arrange — simulate a message consumer: build the serializer from a DbContext,
            // run validation, then persist. No HTTP context anywhere.
            using var dbContext = NewInMemoryContext();
            var serializer = new PerFieldCustomerSerializer(dbContext);
            var data = new CustomerDto { Name = "FromConsumer", CNPJ = "12.345.678/0001-90" };
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Act
            var validated = await serializer.RunValidationAsync(data, context, errors);
            Assert.Empty(errors);
            var created = await serializer.CreateAsync(validated);

            // Assert
            Assert.NotEqual(Guid.Empty, created.Id);
            var persisted = await dbContext.Customer.AsNoTracking()
                .FirstAsync(c => c.Id == created.Id);
            Assert.Equal("FromConsumer", persisted.Name);
            Assert.Equal("12345678000190", persisted.CNPJ);
        }
    }

    public class HeadlessPartialUpdateFromRawJson
    {
        [Fact]
        public async Task PartialJsonObjectStringCtor_DrivesValidationAndPartialUpdate_OutsideHttp()
        {
            // Arrange — seed a Customer, then simulate a consumer that holds a raw JSON
            // body and wants to apply a partial update without going through MVC.
            using var dbContext = NewInMemoryContext();
            var existing = new Customer
            {
                Id = Guid.NewGuid(),
                Name = "Original",
                CNPJ = "99999999000199",
            };
            dbContext.Customer.Add(existing);
            await dbContext.SaveChangesAsync();

            var serializer = new PerFieldCustomerSerializer(dbContext);

            // The raw payload only sets CNPJ; Name must remain "Original".
            var partial = new PartialJsonObject<CustomerDto>("{\"cnpj\":\"12.345.678/0001-90\"}");
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(
                SerializerOperation.PartialUpdate, existing.Id, partial);

            // Act
            await serializer.RunValidationAsync(partial.Instance, context, errors, partial);
            Assert.Empty(errors);
            var updated = await serializer.PartialUpdateAsync(partial, existing.Id);

            // Assert
            Assert.NotNull(updated);
            Assert.Equal("Original", updated!.Name);
            Assert.Equal("12345678000190", updated.CNPJ);
        }
    }
}
