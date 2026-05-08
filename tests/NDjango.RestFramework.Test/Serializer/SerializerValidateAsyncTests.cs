using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NDjango.RestFramework.Test.Serializer;

public class SerializerValidateAsyncTests
{
    public class DefaultPassThrough : IntegrationTests
    {
        [Fact]
        public async Task Post_DefaultSerializer_ShouldReturn201WithoutValidation()
        {
            // Arrange
            // IntAsIdEntities uses the default Serializer<> with no ValidateAsync override
            var entity = new IntAsIdEntityDto { Name = "PassThrough" };
            var content = new StringContent(
                JsonConvert.SerializeObject(entity), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/IntAsIdEntities", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            Assert.Equal("PassThrough", body["name"]?.ToString());
        }
    }

    public class PostHappyPath : IntegrationTests
    {
        [Fact]
        public async Task Post_ValidInput_ShouldReturn201Created()
        {
            // Arrange
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "12345678000190" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            Assert.Equal("ValidName", body["name"]?.ToString());
            Assert.Equal("12345678000190", body["cnpj"]?.ToString());
        }
    }

    public class PostMutation : IntegrationTests
    {
        [Fact]
        public async Task Post_FormattedCNPJ_ShouldNormalizeAndPersistDigitsOnly()
        {
            // Arrange
            var customer = new CustomerDto { Name = "MutationTest", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            var createdId = body["id"]?.ToObject<Guid>();
            Assert.NotNull(createdId);

            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == createdId);
            Assert.Equal("12345678000190", persisted.CNPJ);
        }
    }

    public class PostSingleFieldSingleError : IntegrationTests
    {
        [Fact]
        public async Task Post_InvalidCNPJLength_ShouldReturn400WithExactShape()
        {
            // Arrange
            var customer = new CustomerDto { Name = "ValidName", CNPJ = "123" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errorResponse = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.Equal("VALIDATION_ERRORS", errorResponse.Type);
            Assert.Equal(400, errorResponse.StatusCode);
            Assert.True(errorResponse.Error.ContainsKey("CNPJ"));
            Assert.Contains("CNPJ must have 14 digits.", errorResponse.Error["CNPJ"]);
        }
    }

    public class PostSingleFieldMultipleErrors : IntegrationTests
    {
        [Fact]
        public async Task Post_AllZeroCNPJThatAlreadyExists_ShouldReturnMultipleErrorsOnSameField()
        {
            // Arrange
            // Seed a customer with all-zero 14-digit CNPJ so that posting the same value
            // triggers both the "all zeros" rule and the "already exists" uniqueness rule.
            var existingCnpj = "00000000000000";
            var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", CNPJ = existingCnpj };
            Context.Customer.Add(existing);
            await Context.SaveChangesAsync();

            var newCustomer = new CustomerDto { Name = "ValidName", CNPJ = existingCnpj };
            var content = new StringContent(
                JsonConvert.SerializeObject(newCustomer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.True(errors.Error["CNPJ"].Length >= 2,
                $"Expected at least 2 errors on CNPJ but got {errors.Error["CNPJ"].Length}: " +
                $"[{string.Join(", ", errors.Error["CNPJ"])}]");
            Assert.Contains("CNPJ cannot be all zeros.", errors.Error["CNPJ"]);
            Assert.Contains("Customer with this CNPJ already exists.", errors.Error["CNPJ"]);
        }
    }

    public class PostMultipleFieldErrors : IntegrationTests
    {
        [Fact]
        public async Task Post_NameAndCNPJBothInvalid_ShouldReturnErrorsForBothFields()
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
            Assert.True(errors.Error.ContainsKey("CNPJ"), "Should have CNPJ errors");
            Assert.True(errors.Error.ContainsKey("Name"), "Should have Name errors");
            Assert.Contains("CNPJ must have 14 digits.", errors.Error["CNPJ"]);
            Assert.Contains("Name is required.", errors.Error["Name"]);
        }
    }

    public class PostAsyncUniqueness : IntegrationTests
    {
        [Fact]
        public async Task Post_DuplicateCNPJ_ShouldReturn400WithUniquenessError()
        {
            // Arrange
            var existingCnpj = "12345678000190";
            var existing = new Customer { Id = Guid.NewGuid(), Name = "Existing", CNPJ = existingCnpj };
            Context.Customer.Add(existing);
            await Context.SaveChangesAsync();

            var customer = new CustomerDto { Name = "NewCustomer", CNPJ = "12.345.678/0001-90" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.Contains("Customer with this CNPJ already exists.", errors.Error["CNPJ"]);
        }
    }

    public class PutSkipSelfUniqueness : IntegrationTests
    {
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
            var response = await Client.PutAsync($"api/ValidatingCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("UpdatedName", persisted.Name);
            Assert.Equal(cnpj, persisted.CNPJ);
        }
    }

    public class PutMutationAndValid : IntegrationTests
    {
        [Fact]
        public async Task Put_FormattedCNPJ_ShouldNormalizeAndReturn200()
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
            Assert.Equal("UpdatedName", persisted.Name);
            Assert.Equal("12345678000190", persisted.CNPJ);
        }
    }

    public class PatchOnlyValidateSent : IntegrationTests
    {
        [Fact]
        public async Task Patch_OnlyNameSent_ShouldSkipCNPJValidationAndReturn200()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "short" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patchBody = new { Name = "UpdatedName" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/ValidatingCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("UpdatedName", persisted.Name);
            Assert.Equal("short", persisted.CNPJ);
        }
    }

    public class PatchValidateSentAndFail : IntegrationTests
    {
        [Fact]
        public async Task Patch_InvalidCNPJSent_ShouldReturn400()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "12345678000190" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patchBody = new { CNPJ = "bad" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patchBody), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/ValidatingCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("CNPJ"));
            Assert.Contains("CNPJ must have 14 digits.", errors.Error["CNPJ"]);
        }
    }

    public class PatchMutationPersists : IntegrationTests
    {
        [Fact]
        public async Task Patch_FormattedCNPJ_ShouldNormalizeAndPersistWithOtherFieldsUnchanged()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "99999999000199", Age = 42 };
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
            Assert.Equal(42, persisted.Age);
        }
    }

    public class DataAnnotationsLayering : IntegrationTests
    {
        [Fact]
        public async Task Post_FailingDataAnnotations_ShouldReturn400BeforeValidateAsync()
        {
            // Arrange
            // Name = "ab" (length 2) fails MinLength(3) DataAnnotation on CustomerDto.
            // ValidateAsync should never be reached.
            var customer = new CustomerDto { Name = "ab", CNPJ = "12345678000190" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/ValidatingCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var errors = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.True(errors.Error.ContainsKey("Name"));
            Assert.Contains("Name should have at least 3 characters", errors.Error["Name"]);
        }
    }

    /// <summary>
    /// The HTTP <c>PutMany</c> action was removed (DRF refuses to ship a bulk-update verb).
    /// <see cref="Serializer{TOrigin,TDestination,TPrimaryKey,TContext}.UpdateManyAsync"/> is
    /// retained as a headless primitive, so these tests drive it directly with the same
    /// validation pipeline a non-HTTP caller would compose. Uses an in-memory DbContext to
    /// avoid spinning up the WebApplicationFactory and SQL Server database (the validation
    /// path doesn't exercise relational-only EF features).
    /// </summary>
    public class UpdateManyAsyncValidation
    {
        private static AppDbContext NewInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task UpdateManyAsync_InvalidPayload_ShouldShortCircuitAndLeaveEntitiesUntouched()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var customer1 = new Customer { Id = Guid.NewGuid(), Name = "One", CNPJ = "11111111111111" };
            var customer2 = new Customer { Id = Guid.NewGuid(), Name = "Two", CNPJ = "22222222222222" };
            dbContext.Customer.AddRange(customer1, customer2);
            await dbContext.SaveChangesAsync();

            var serializer = new ValidatingCustomerSerializer(dbContext);
            var body = new CustomerDto { Name = null, CNPJ = "bad" };
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(SerializerOperation.BulkUpdate, default);

            // Act
            await serializer.RunValidationAsync(body, context, errors);

            // Assert
            Assert.True(errors.ContainsKey("CNPJ"));
            Assert.True(errors.ContainsKey("Name"));
            Assert.Contains("CNPJ must have 14 digits.", errors["CNPJ"]);
            Assert.Contains("Name is required.", errors["Name"]);

            // The headless caller is responsible for short-circuiting on errors — assert that
            // when it does, no rows were touched.
            var persisted1 = dbContext.Customer.AsNoTracking().First(c => c.Id == customer1.Id);
            var persisted2 = dbContext.Customer.AsNoTracking().First(c => c.Id == customer2.Id);
            Assert.Equal("One", persisted1.Name);
            Assert.Equal("Two", persisted2.Name);
        }

        [Fact]
        public async Task UpdateManyAsync_ValidPayload_ShouldRunValidationAndPersistNormalizedFields()
        {
            // Arrange
            using var dbContext = NewInMemoryContext();
            var customer1 = new Customer { Id = Guid.NewGuid(), Name = "One", CNPJ = "11111111111111" };
            var customer2 = new Customer { Id = Guid.NewGuid(), Name = "Two", CNPJ = "22222222222222" };
            dbContext.Customer.AddRange(customer1, customer2);
            await dbContext.SaveChangesAsync();

            var serializer = new ValidatingCustomerSerializer(dbContext);
            var body = new CustomerDto { Name = "BulkUpdate", CNPJ = "33.333.333/0001-33" };
            var errors = new Dictionary<string, List<string>>();
            var context = new ValidationContext<Guid>(SerializerOperation.BulkUpdate, default);

            // Act
            var validated = await serializer.RunValidationAsync(body, context, errors);
            Assert.Empty(errors);
            var updatedIds = await serializer.UpdateManyAsync(
                dbContext.Customer,
                validated,
                new[] { customer1.Id, customer2.Id });

            // Assert
            Assert.Equal(2, updatedIds.Count);
            Assert.Contains(customer1.Id, updatedIds);
            Assert.Contains(customer2.Id, updatedIds);

            var persisted1 = dbContext.Customer.AsNoTracking().First(c => c.Id == customer1.Id);
            Assert.Equal("33333333000133", persisted1.CNPJ);
            Assert.Equal("BulkUpdate", persisted1.Name);
        }
    }
}
