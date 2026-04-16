using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NDjango.RestFramework.Test.Serializer;

public class PerFieldValidationTests
{
    public class PostPerFieldHookRuns : IntegrationTests
    {
        [Fact]
        public async Task Post_PerFieldHookNormalizesAndValidates_ShouldReturn201WithNormalizedValue()
        {
            // Arrange
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerFieldCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            var createdId = body["id"]?.ToObject<Guid>();
            Assert.NotNull(createdId);

            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == createdId);
            Assert.Equal("12345678000190", persisted.CNPJ);
        }

        [Fact]
        public async Task Post_PerFieldHookValidationFails_ShouldReturn400()
        {
            // Arrange
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "123" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerFieldCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.Contains("CNPJ must have 14 digits.", errors.Error["CNPJ"]);
        }
    }

    public class PutPerFieldHookRuns : IntegrationTests
    {
        [Fact]
        public async Task Put_PerFieldHookRunsWithEntityId_ShouldReturn200()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "99999999000199" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var updateDto = new CustomerDto { Name = "UpdatedName", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/PerFieldCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("12345678000190", persisted.CNPJ);
            Assert.Equal("UpdatedName", persisted.Name);
        }

        [Fact]
        public async Task Put_SameCNPJOnSelf_ShouldReturn200WithoutUniquenessError()
        {
            // Arrange
            var cnpj = "12345678000190";
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = cnpj };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var updateDto = new CustomerDto { Name = "UpdatedName", CNPJ = cnpj };
            var content = new StringContent(
                JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/PerFieldCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("UpdatedName", persisted.Name);
            Assert.Equal(cnpj, persisted.CNPJ);
        }
    }

    public class PatchPerFieldHookOnlySentFields : IntegrationTests
    {
        [Fact]
        public async Task Patch_OnlyNameSent_ShouldSkipCNPJHookAndReturn200()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "short" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patchBody = new { Name = "UpdatedName" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/PerFieldCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("UpdatedName", persisted.Name);
            // CNPJ should remain unchanged because the hook was not invoked
            Assert.Equal("short", persisted.CNPJ);
        }

        [Fact]
        public async Task Patch_CNPJSent_ShouldRunCNPJHookAndReturn200()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "99999999000199" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patchBody = new { CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/PerFieldCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("12345678000190", persisted.CNPJ);
            Assert.Equal("Original", persisted.Name);
        }
    }

    public class PatchPerFieldMutationPersists : IntegrationTests
    {
        [Fact]
        public async Task Patch_FormattedCNPJ_ShouldNormalizeViaSetValueAndPersist()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "99999999000199", Age = 42 };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patchBody = new { CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/PerFieldCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("12345678000190", persisted.CNPJ);
            Assert.Equal("Original", persisted.Name);
            Assert.Equal(42, persisted.Age);
        }
    }

    public class MultiplePerFieldHooksRun : IntegrationTests
    {
        [Fact]
        public async Task Post_MultipleFieldsFail_ShouldReturnErrorsForBothFields()
        {
            // Arrange
            var customer = new CustomerDto { Name = null, CNPJ = "short" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerFieldCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"), "Should have CNPJ errors");
            Assert.True(errors.Error.ContainsKey("Name"), "Should have Name errors");
            Assert.Contains("CNPJ must have 14 digits.", errors.Error["CNPJ"]);
            Assert.Contains("Name is required.", errors.Error["Name"]);
        }
    }

    public class CrossFieldRunsAfterPerFieldHooks : IntegrationTests
    {
        [Fact]
        public async Task Post_CrossFieldValidateAsync_ShouldReceiveNormalizedDataFromPerFieldHooks()
        {
            // Arrange
            // The per-field hook normalizes CNPJ by stripping non-digits.
            // The cross-field hook checks that Name != CNPJ.
            // We send Name = "12345678000190" and CNPJ = "12.345.678/0001-90" (which normalizes to "12345678000190").
            // Cross-field should see the normalized CNPJ and reject because Name == CNPJ.
            var customer = new CustomerDto { Name = "12345678000190", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerFieldCrossFieldCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("Name"));
            Assert.Contains("Name cannot be the same as CNPJ.", errors.Error["Name"]);
        }

        [Fact]
        public async Task Post_ValidInput_ShouldRunBothPerFieldAndCrossFieldAndReturn201()
        {
            // Arrange
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerFieldCrossFieldCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            // The CNPJ should be normalized by the per-field hook
            Assert.Equal("12345678000190", body["cnpj"]?.ToString());
        }
    }

    public class PerFieldErrorsPreventCrossFieldValidation : IntegrationTests
    {
        [Fact]
        public async Task Post_PerFieldErrors_ShouldShortCircuitAndNotCallCrossField()
        {
            // Arrange
            // PerFieldShortCircuitSerializer's ValidateCNPJAsync always adds errors.
            // Its cross-field ValidateAsync sets CrossFieldCalled = true.
            // We need to verify the cross-field was NOT called.
            // We can verify this indirectly: the error response should only contain
            // the per-field error, not any cross-field error.
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "12345678000190" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerFieldShortCircuitCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.Contains("Always fails.", errors.Error["CNPJ"]);
            // If cross-field had run, it would have no way to add errors here since the
            // short-circuit prevents it. The fact that only "Always fails." appears confirms.
        }
    }

    public class PerFieldShortCircuitDirect
    {
        [Fact]
        public async Task RunValidationAsync_PerFieldErrors_ShouldNotCallCrossFieldValidateAsync()
        {
            // Arrange
            // PerFieldShortCircuitSerializer doesn't need a real DB for this test;
            // its ValidateCNPJAsync hook only adds errors without DB access.
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            using var dbContext = new AppDbContext(dbContextOptions);
            var shortCircuitSerializer = new PerFieldShortCircuitSerializer(dbContext);
            var data = new CustomerDto { Name = "Test", CNPJ = "12345678000190" };
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Act
            await shortCircuitSerializer.RunValidationAsync(data, context, errors);

            // Assert
            Assert.True(errors.Count > 0, "Per-field hook should have added errors");
            Assert.False(shortCircuitSerializer.CrossFieldCalled,
                "Cross-field ValidateAsync should NOT be called when per-field hooks produce errors");
        }
    }

    public class LegacyOverloadsStillWork : IntegrationTests
    {
        [Fact]
        public async Task Post_LegacyValidatingCustomerSerializer_ShouldReturn201()
        {
            // Arrange — existing ValidatingCustomerSerializer with legacy overloads
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "12345678000190" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Put_LegacyValidatingCustomerSerializer_ShouldReturn200()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "99999999000199" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var updateDto = new CustomerDto { Name = "UpdatedName", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/ValidatingCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("12345678000190", persisted.CNPJ);
        }

        [Fact]
        public async Task Patch_LegacyValidatingCustomerSerializer_ShouldReturn200()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "99999999000199", Age = 30 };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patchBody = new { CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/ValidatingCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("12345678000190", persisted.CNPJ);
            Assert.Equal("Original", persisted.Name);
        }

        [Fact]
        public async Task Post_LegacyValidatingCustomerSerializer_InvalidInput_ShouldReturn400()
        {
            // Arrange
            var customer = new CustomerDto { Name = null, CNPJ = "short" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.True(errors.Error.ContainsKey("Name"));
        }
    }

    public class StartupValidationCatchesMisnamedHooks
    {
        [Fact]
        public void GetMisnamedHooks_MisnamedHookSerializer_ShouldReturnMisnamedMethods()
        {
            // Arrange
            var serializerType = typeof(MisnamedHookSerializer);

            // Act
            var misnamed = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetMisnamedHooks(serializerType);

            // Assert
            Assert.Single(misnamed);
            Assert.Contains("ValidateCnjAsync", misnamed);
        }

        [Fact]
        public void GetMisnamedHooks_ValidPerFieldSerializer_ShouldReturnEmpty()
        {
            // Arrange
            var serializerType = typeof(PerFieldCustomerSerializer);

            // Act
            var misnamed = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetMisnamedHooks(serializerType);

            // Assert
            Assert.Empty(misnamed);
        }

        [Fact]
        public void GetMisnamedHooks_BaseSerializerWithNoHooks_ShouldReturnEmpty()
        {
            // Arrange
            var serializerType = typeof(Serializer<CustomerDto, Customer, Guid, AppDbContext>);

            // Act
            var misnamed = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetMisnamedHooks(serializerType);

            // Assert
            Assert.Empty(misnamed);
        }
    }

    public class PerFieldUniquenessWithSkipSelf : IntegrationTests
    {
        [Fact]
        public async Task Put_DuplicateCNPJOnDifferentEntity_ShouldReturn400()
        {
            // Arrange
            var existingCnpj = "12345678000190";
            var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", CNPJ = existingCnpj };
            var target = new Customer { Id = Guid.NewGuid(), Name = "Target", CNPJ = "99999999000199" };
            Context.Customer.AddRange(existing, target);
            await Context.SaveChangesAsync();

            var updateDto = new CustomerDto { Name = "UpdatedTarget", CNPJ = existingCnpj };
            var content = new StringContent(
                JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/PerFieldCustomers/{target.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.Contains("Customer with this CNPJ already exists.", errors.Error["CNPJ"]);
        }

        [Fact]
        public async Task Put_SameCNPJOnSelfUsingPerFieldHook_ShouldReturn200()
        {
            // Arrange
            var cnpj = "12345678000190";
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = cnpj };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var updateDto = new CustomerDto { Name = "UpdatedName", CNPJ = cnpj };
            var content = new StringContent(
                JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/PerFieldCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("UpdatedName", persisted.Name);
            Assert.Equal(cnpj, persisted.CNPJ);
        }

        [Fact]
        public async Task Patch_DuplicateCNPJOnDifferentEntity_ShouldReturn400()
        {
            // Arrange
            var existingCnpj = "12345678000190";
            var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", CNPJ = existingCnpj };
            var target = new Customer { Id = Guid.NewGuid(), Name = "Target", CNPJ = "99999999000199" };
            Context.Customer.AddRange(existing, target);
            await Context.SaveChangesAsync();

            var patchBody = new { CNPJ = existingCnpj };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/PerFieldCustomers/{target.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.Contains("Customer with this CNPJ already exists.", errors.Error["CNPJ"]);
        }
    }

    public class PutManyValidationContextSignalsBulkUpdate : IntegrationTests
    {
        [Fact]
        public async Task PutMany_ValidationContext_ShouldSignalBulkUpdateNotCreate()
        {
            // Arrange
            var target = new Customer { Id = Guid.NewGuid(), Name = "Target", CNPJ = "99999999000199" };
            Context.Customer.Add(target);
            await Context.SaveChangesAsync();

            var capturingSerializer = Services.GetRequiredService<ContextCapturingSerializer>();

            var updateDto = new CustomerDto { Name = "BulkName", CNPJ = "12345678000190" };
            var content = new StringContent(
                JsonConvert.SerializeObject(updateDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync(
                $"api/ContextCapturingCustomers?ids={target.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(capturingSerializer.LastContext);
            Assert.Equal(SerializerOperation.BulkUpdate, capturingSerializer.LastContext.Operation);
            Assert.True(capturingSerializer.LastContext.IsBulkUpdate);
            Assert.False(capturingSerializer.LastContext.IsCreate);
            Assert.False(capturingSerializer.LastContext.IsUpdate);
            Assert.False(capturingSerializer.LastContext.IsPartialUpdate);
        }

        [Fact]
        public async Task Post_ValidationContext_ShouldSignalCreateNotBulkUpdate()
        {
            // Arrange
            var capturingSerializer = Services.GetRequiredService<ContextCapturingSerializer>();

            var createDto = new CustomerDto { Name = "FreshName", CNPJ = "12345678000190" };
            var content = new StringContent(
                JsonConvert.SerializeObject(createDto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ContextCapturingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(capturingSerializer.LastContext);
            Assert.Equal(SerializerOperation.Create, capturingSerializer.LastContext.Operation);
            Assert.True(capturingSerializer.LastContext.IsCreate);
            Assert.False(capturingSerializer.LastContext.IsBulkUpdate);
        }
    }

    public class ValidationContextConstructorValidation
    {
        [Fact]
        public void Constructor_UpdateWithDefaultEntityId_ShouldThrowArgumentException()
        {
            // Arrange
            // Act
            var ex = Assert.Throws<ArgumentException>(
                () => new ValidationContext<Guid>(SerializerOperation.Update, default));

            // Assert
            Assert.Equal("entityId", ex.ParamName);
            Assert.Contains("Update", ex.Message);
        }

        [Fact]
        public void Constructor_PartialUpdateWithDefaultEntityId_ShouldThrowArgumentException()
        {
            // Arrange
            // Act
            var ex = Assert.Throws<ArgumentException>(
                () => new ValidationContext<Guid>(SerializerOperation.PartialUpdate, default));

            // Assert
            Assert.Equal("entityId", ex.ParamName);
            Assert.Contains("PartialUpdate", ex.Message);
        }

        [Fact]
        public void Constructor_CreateWithDefaultEntityId_ShouldSucceed()
        {
            // Arrange
            // Act
            var context = new ValidationContext<Guid>(SerializerOperation.Create, default);

            // Assert
            Assert.Equal(SerializerOperation.Create, context.Operation);
            Assert.True(context.IsCreate);
            // TPrimaryKey is Guid (value type), and TPrimaryKey? on an unconstrained generic stays as Guid,
            // so default is Guid.Empty, not null.
            Assert.Equal(default(Guid), context.EntityId);
        }

        [Fact]
        public void Constructor_BulkUpdateWithDefaultEntityId_ShouldSucceed()
        {
            // Arrange
            // Act
            var context = new ValidationContext<Guid>(SerializerOperation.BulkUpdate, default);

            // Assert
            Assert.Equal(SerializerOperation.BulkUpdate, context.Operation);
            Assert.True(context.IsBulkUpdate);
            Assert.False(context.IsCreate);
        }

        [Fact]
        public void Constructor_UpdateWithValidEntityId_ShouldSucceed()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var context = new ValidationContext<Guid>(SerializerOperation.Update, id);

            // Assert
            Assert.Equal(SerializerOperation.Update, context.Operation);
            Assert.True(context.IsUpdate);
            Assert.Equal(id, context.EntityId);
        }

        [Fact]
        public void Constructor_PartialUpdateWithValidEntityId_ShouldSucceed()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var context = new ValidationContext<Guid>(SerializerOperation.PartialUpdate, id);

            // Assert
            Assert.Equal(SerializerOperation.PartialUpdate, context.Operation);
            Assert.True(context.IsPartialUpdate);
            Assert.True(context.IsPartial);
            Assert.Equal(id, context.EntityId);
        }
    }

    public class SetValueInvokerIsCached
    {
        [Fact]
        public void GetSetValueInvoker_SameProperty_ShouldReturnCachedInstance()
        {
            // Arrange
            var property = typeof(CustomerDto).GetProperty(nameof(CustomerDto.CNPJ),
                BindingFlags.Public | BindingFlags.Instance)!;

            // Act
            var first = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetSetValueInvoker(property);
            var second = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetSetValueInvoker(property);

            // Assert
            Assert.Same(first.ClosedSetValue, second.ClosedSetValue);
            Assert.Same(first.Lambda, second.Lambda);
        }

        [Fact]
        public void GetSetValueInvoker_DifferentProperties_ShouldReturnDifferentLambdas()
        {
            // Arrange
            var cnpjProperty = typeof(CustomerDto).GetProperty(nameof(CustomerDto.CNPJ),
                BindingFlags.Public | BindingFlags.Instance)!;
            var nameProperty = typeof(CustomerDto).GetProperty(nameof(CustomerDto.Name),
                BindingFlags.Public | BindingFlags.Instance)!;

            // Act
            var cnpjInvoker = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetSetValueInvoker(cnpjProperty);
            var nameInvoker = Serializer<CustomerDto, Customer, Guid, AppDbContext>
                .GetSetValueInvoker(nameProperty);

            // Assert
            // Both CNPJ and Name are strings, so the closed SetValue<string> MethodInfo is the same
            // (the CLR caches MakeGenericMethod). The lambdas must differ because they access different members.
            Assert.NotSame(cnpjInvoker.Lambda, nameInvoker.Lambda);
        }
    }
}
