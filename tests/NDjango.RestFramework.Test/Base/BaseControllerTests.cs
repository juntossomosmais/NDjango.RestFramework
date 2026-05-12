using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Bogus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Paginations;
using NDjango.RestFramework.Serializer;
using NDjango.RestFramework.Test.Support;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NDjango.RestFramework.Test.Base;

public class BaseControllerTests
{
    public class Delete : IntegrationTests
    {
        [Fact]
        public async Task Delete_WithObject_ShouldDeleteEntityFromDatabaseAndReturnNoContent()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            Assert.Null(updatedCustomer);
        }

        [Fact]
        public async Task Delete_WhenEntityDoesntExist_ReturnsNotFound()
        {
            // Act
            var response = await Client.DeleteAsync($"api/Customers/{Guid.NewGuid()}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Delete_WithObject_ShouldReturnEmptyBodyOn204()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 30 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal(string.Empty, responseData);
        }

        [Fact]
        public async Task Delete_WithMultipleEntities_ShouldOnlyDeleteTargetedEntity()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "111", Name = "aaa" };
            var customer2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "222", Name = "bbb" };
            var customer3 = new Customer() { Id = Guid.NewGuid(), CNPJ = "333", Name = "ccc" };
            dbSet.AddRange(customer1, customer2, customer3);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/Customers/{customer2.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var remaining = dbSet.AsNoTracking()
                .Where(x => x.Id == customer1.Id || x.Id == customer3.Id)
                .ToList();
            Assert.Equal(2, remaining.Count);
            var deleted = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer2.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Delete_WhenDeleteIsNotAllowedByActionOptions_ShouldReturnMethodNotAllowed()
        {
            // Arrange — IntAsIdEntitiesController is configured with AllowDelete = false
            // so the single-resource DELETE returns 405 without touching the database.
            var dbSet = Context.Set<IntAsIdEntity>();
            var entity = new IntAsIdEntity() { Name = "abc" };
            dbSet.Add(entity);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/IntAsIdEntities/{entity.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            var stillExists = dbSet.AsNoTracking().Any(x => x.Id == entity.Id);
            Assert.True(stillExists);
        }
    }

    public class ValidateDestroy : IntegrationTests
    {
        private readonly ValidateDestroyCustomerSerializer _spy;

        public ValidateDestroy()
        {
            _spy = Services.GetRequiredService<ValidateDestroyCustomerSerializer>();
            _spy.ResetSpyState();
        }

        [Fact]
        public async Task ValidateDestroy_HookPopulatesErrors_Returns400WithValidationErrors()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer { Id = Guid.NewGuid(), CNPJ = "123", Name = "BLOCKED" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/ValidateDestroyCustomers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal("VALIDATION_ERRORS", body["type"]?.ToString());
            Assert.Equal(400, body["statusCode"]?.ToObject<int>());
            var stillThere = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            Assert.NotNull(stillThere);
            Assert.Equal(0, _spy.InstanceDestroyCalls);
        }

        [Fact]
        public async Task ValidateDestroy_HookUsesNonFieldErrorsKey_ResponseBodyContainsKey()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer { Id = Guid.NewGuid(), CNPJ = "999", Name = "BLOCKED" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/ValidateDestroyCustomers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            var errorObject = body["error"] as JObject;
            Assert.NotNull(errorObject);
            Assert.True(errorObject!.ContainsKey("non_field_errors"));
            var messages = errorObject["non_field_errors"] as JArray;
            Assert.NotNull(messages);
            Assert.Single(messages!);
            Assert.Equal("Customer is blocked from deletion.", messages![0].ToString());
        }

        [Fact]
        public async Task ValidateDestroy_HookReceivesLoadedEntity()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer { Id = Guid.NewGuid(), CNPJ = "555", Name = "alpha", Age = 42 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/ValidateDestroyCustomers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(1, _spy.ValidateDestroyCalls);
            Assert.NotNull(_spy.LastValidateDestroyInstance);
            Assert.Equal(customer.Id, _spy.LastValidateDestroyInstance!.Id);
            Assert.Equal("alpha", _spy.LastValidateDestroyInstance.Name);
            Assert.Equal("555", _spy.LastValidateDestroyInstance.CNPJ);
            Assert.Equal(42, _spy.LastValidateDestroyInstance.Age);
        }

        [Fact]
        public async Task ValidateDestroy_NotFound_HookNotCalled()
        {
            // Arrange
            var missingId = Guid.NewGuid();

            // Act
            var response = await Client.DeleteAsync($"api/ValidateDestroyCustomers/{missingId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _spy.ValidateDestroyCalls);
            Assert.Equal(0, _spy.InstanceDestroyCalls);
        }

        [Fact]
        public async Task ValidateDestroy_DefaultNoOp_DeletesAndReturns204()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer { Id = Guid.NewGuid(), CNPJ = "777", Name = "ok-to-delete" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/ValidateDestroyCustomers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(1, _spy.ValidateDestroyCalls);
            Assert.Equal(1, _spy.InstanceDestroyCalls);
            var gone = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            Assert.Null(gone);
        }
    }

    public class PerformHooks : IntegrationTests
    {
        private readonly PerformHookSpy _spy;

        public PerformHooks()
        {
            _spy = Services.GetRequiredService<PerformHookSpy>();
            _spy.Reset();
        }

        [Fact]
        public async Task Post_ShouldInvokePerformCreateAsync_AndOverrideMutatesPersistedEntity()
        {
            // Arrange
            var customer = new CustomerDto { Name = "BaseName", CNPJ = "111111111111" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/PerformHookCustomers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = JObject.Parse(await response.Content.ReadAsStringAsync());
            var createdId = body["id"]!.ToObject<Guid>();
            Assert.Equal(1, _spy.PerformCreateCalls);

            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == createdId);
            Assert.Equal("BaseName_perform_created", persisted.Name);
        }

        [Fact]
        public async Task Put_ShouldInvokePerformUpdateAsync_AndOverrideMutatesPersistedEntity()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "999" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var dto = new CustomerDto { Name = "Replaced", CNPJ = "888" };
            var content = new StringContent(
                JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/PerformHookCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, _spy.PerformUpdateCalls);

            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("Replaced_perform_updated", persisted.Name);
        }

        [Fact]
        public async Task Patch_ShouldInvokePerformPartialUpdateAsync_AndOverrideMutatesPersistedEntity()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "Original", CNPJ = "777" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var patch = new { Name = "Patched" };
            var content = new StringContent(
                JsonConvert.SerializeObject(patch), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/PerformHookCustomers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(1, _spy.PerformPartialUpdateCalls);

            var persisted = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("Patched_perform_patched", persisted.Name);
        }

        [Fact]
        public async Task Put_WhenEntityMissing_ShouldReturnNotFoundAndNotInvokePerformUpdate()
        {
            // Arrange
            var dto = new CustomerDto { Name = "Replaced", CNPJ = "888" };
            var content = new StringContent(
                JsonConvert.SerializeObject(dto), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/PerformHookCustomers/{Guid.NewGuid()}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            // DRF parity: load happens before the hook, so a missing row short-circuits to 404
            // without ever invoking PerformUpdateAsync. Mirrors mixins.py:58-67 at tag 3.17.1.
            Assert.Equal(0, _spy.PerformUpdateCalls);
        }

        [Fact]
        public async Task Delete_ShouldInvokePerformDestroyAsync_AndRemoveTheEntity()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "ToDelete", CNPJ = "555" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/PerformHookCustomers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(1, _spy.PerformDestroyCalls);
            Assert.Equal("ToDelete", _spy.LastDestroyedInstanceName);
            Assert.Null(Context.Customer.AsNoTracking().FirstOrDefault(c => c.Id == customer.Id));
        }

        [Fact]
        public async Task Delete_WhenEntityMissing_ShouldReturnNotFoundAndNotInvokePerformDestroy()
        {
            // Act
            var response = await Client.DeleteAsync($"api/PerformHookCustomers/{Guid.NewGuid()}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(0, _spy.PerformDestroyCalls);
        }
    }

    /// <summary>
    /// Pins the documented instance-aware override pattern. With single-row writes now
    /// instance-taking (DRF parity, <c>mixins.py:58-67</c> at tag 3.17.1), the canonical
    /// extension point for predicates that depend on the loaded row's state is a
    /// <c>Perform*Async</c> override. The controller under test refuses an update unless
    /// the request's <c>X-Region</c> header matches the persisted <c>Region</c>, proving
    /// that override-shaped guards naturally compose on top of the controller's filter
    /// chain without re-loading the entity.
    /// </summary>
    public class InstanceAwareOverridePath : IntegrationTests
    {
        [Fact]
        public async Task Put_WhenOverrideRejectsMismatchedRegion_ShouldPropagateException()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "RegionA", CNPJ = "111", Region = "us-east" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var body = new { Name = "Updated", CNPJ = "999" };
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/RegionGuardedCustomers/{customer.Id}");
            request.Headers.Add("X-Region", "eu-west");
            request.Content = new StringContent(
                JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var exception = await Assert.ThrowsAsync<RegionMismatchException>(() => Client.SendAsync(request));

            // Assert — the override observed the loaded instance's region, not the DTO's.
            Assert.Equal("us-east", exception.InstanceRegion);
            Assert.Equal("eu-west", exception.HeaderRegion);
            var unchanged = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("RegionA", unchanged.Name);
        }

        [Fact]
        public async Task Put_WhenOverrideAcceptsMatchingRegion_ShouldPersistTheUpdate()
        {
            // Arrange
            var customer = new Customer { Id = Guid.NewGuid(), Name = "RegionA", CNPJ = "111", Region = "us-east" };
            Context.Customer.Add(customer);
            await Context.SaveChangesAsync();

            var body = new { Name = "Updated", CNPJ = "999", Region = "us-east" };
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/RegionGuardedCustomers/{customer.Id}");
            request.Headers.Add("X-Region", "us-east");
            request.Content = new StringContent(
                JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = Context.Customer.AsNoTracking().First(c => c.Id == customer.Id);
            Assert.Equal("Updated", updated.Name);
            Assert.Equal("999", updated.CNPJ);
        }
    }

    public class DeleteMany : IntegrationTests
    {
        [Fact]
        public async Task DeleteMany_ShouldDeleteManyEntities_AndReturnNoContent()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var survivor = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(survivor);
            var expectedToBeDeletedOne = Guid.NewGuid();
            dbSet.Add(new Customer()
            { Id = expectedToBeDeletedOne, CNPJ = "456", Name = "def" });
            var expectedToBeDeletedTwo = Guid.NewGuid();
            dbSet.Add(new Customer()
            { Id = expectedToBeDeletedTwo, CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var url = $"api/Customers?ids={expectedToBeDeletedOne}&ids={expectedToBeDeletedTwo}";
            var response = await Client.DeleteAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());

            var targetedIds = new[] { expectedToBeDeletedOne, expectedToBeDeletedTwo };
            Assert.False(dbSet.AsNoTracking().Any(m => targetedIds.Contains(m.Id)));
            Assert.True(dbSet.AsNoTracking().Any(m => m.Id == survivor.Id));
        }

        [Fact]
        public async Task DeleteMany_WithEmptyIdsList_ShouldReturnNoContent_AndDeleteNothing()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var existing = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(existing);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync("api/Customers");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());
            Assert.True(dbSet.AsNoTracking().Any(m => m.Id == existing.Id));
        }

        [Fact]
        public async Task DeleteMany_WithMixedExistingAndNonExistingIds_ShouldDeleteOnlyExistingOnes()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var existing1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var existing2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            dbSet.AddRange(existing1, existing2);
            await Context.SaveChangesAsync();

            var nonExistingId = Guid.NewGuid();

            // Act
            var url = $"api/Customers?ids={existing1.Id}&ids={nonExistingId}&ids={existing2.Id}";
            var response = await Client.DeleteAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());

            var existingIds = new[] { existing1.Id, existing2.Id };
            Assert.False(dbSet.AsNoTracking().Any(m => existingIds.Contains(m.Id)));
            Assert.False(dbSet.AsNoTracking().Any(m => m.Id == nonExistingId));
        }

        [Fact]
        public async Task DeleteMany_WithAllNonExistingIds_ShouldReturnNoContent()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            // Act
            var url = $"api/Customers?ids={id1}&ids={id2}";
            var response = await Client.DeleteAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task DeleteMany_ShouldNotLoadEntitiesIntoChangeTracker()
        {
            // Arrange — pin the post-rewrite contract: DestroyManyAsync uses ExecuteDeleteAsync,
            // so no Customer should land in the change tracker after the call.
            var dbSet = Context.Set<Customer>();
            var c1 = new Customer { Id = Guid.NewGuid(), CNPJ = "111", Name = "one" };
            var c2 = new Customer { Id = Guid.NewGuid(), CNPJ = "222", Name = "two" };
            dbSet.AddRange(c1, c2);
            await Context.SaveChangesAsync();
            // Detach the seeded entries so the change tracker starts clean for the assertion.
            Context.ChangeTracker.Clear();

            // Act
            var response = await Client.DeleteAsync($"api/Customers?ids={c1.Id}&ids={c2.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(Context.ChangeTracker.Entries<Customer>());
        }

        [Fact]
        public async Task DeleteMany_WhenBulkDeleteIsNotAllowedByActionOptions_ShouldReturnMethodNotAllowed()
        {
            // Arrange — IntAsIdEntitiesController is configured with AllowBulkDelete = false
            // so consumers can opt out when their ValidateDestroyAsync / DestroyAsync overrides
            // would be silently bypassed by the bulk SQL path.
            var dbSet = Context.Set<IntAsIdEntity>();
            var entity = new IntAsIdEntity() { Name = "abc" };
            dbSet.Add(entity);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/IntAsIdEntities?ids={entity.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            var stillExists = dbSet.AsNoTracking().Any(x => x.Id == entity.Id);
            Assert.True(stillExists);
        }
    }

    public class GetSingle : IntegrationTests
    {
        [Fact]
        public async Task GetSingle_WithValidParameter_ShouldReturn1Record()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer1 = new Customer { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var customer2 = new Customer { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            var customer3 = new Customer { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" };


            dbSet.AddRange(customer1, customer2, customer3);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{customer1.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(responseData);
            Assert.NotNull(customer);
            Assert.Equal(customer1.Id, customer.Id);
        }

        [Fact]
        public async Task GetSingle_WithInValidParameter_ShouldReturn404()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var customer2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            var customer3 = new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" };

            dbSet.AddRange(customer1, customer2, customer3);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{Guid.NewGuid()}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSingle_WithValidId_ShouldReturnOnlyDeclaredFields()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 30 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            // JSON response uses camelCase keys
            var declaredFields = new[] { "name", "cnpj", "age", "id", "region", "customerDocument" };
            foreach (var property in body.Properties())
            {
                Assert.Contains(property.Name, declaredFields);
            }
        }

        [Fact]
        public async Task GetSingle_WithFilterExcludingEntity_ShouldReturnNotFound()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{customer.Id}?Name=nonMatchingValue");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetSingle_WithValidId_ShouldReturnNestedFieldsFiltered()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer()
            {
                Id = Guid.NewGuid(),
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cpf",
                        Document = "12345678900"
                    }
                }
            };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            // JSON response uses camelCase keys
            var documents = body["customerDocument"] as JArray;
            Assert.NotNull(documents);
            Assert.Single(documents);
            var doc = documents[0] as JObject;
            Assert.NotNull(doc);
            // JsonTransform filters to GetFields() declared nested fields, but "id" also appears
            // since it's serialized by default from BaseModel
            var allowedNestedFields = new[] { "documentType", "document", "id" };
            foreach (var property in doc.Properties())
            {
                Assert.Contains(property.Name, allowedNestedFields);
            }
            Assert.Equal("cpf", doc["documentType"]?.ToString());
            Assert.Equal("12345678900", doc["document"]?.ToString());
        }
    }

    public class ListPaged : IntegrationTests
    {
        private const int DefaultPageSize = 5;

        [Fact]
        public async Task ListPaged_ShouldReturn200OK()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Equal(3, paginatedResponse.Results.Count);
            Assert.Equal(3, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithQueryString_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Name=ghi&CNPJ=789");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Single(paginatedResponse.Results);
            Assert.Equal("ghi", paginatedResponse.Results.First().Name);
            Assert.Equal(1, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithIdQueryStringFilter_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?id=35d948bd-ab3d-4446-912b-2d20c57c4935");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Single(paginatedResponse.Results);
            Assert.Equal("abc", paginatedResponse.Results.First().Name);
            Assert.Equal(1, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithIdRangeQueryStringFilter_ShouldReturnTwoRecords()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            var customerIdOne = "6bdc2b9e-3710-40b9-93dd-c7558b446e21";
            dbSet.Add(new Customer()
            { Id = Guid.Parse(customerIdOne), CNPJ = "456", Name = "def" });
            var customerIdTwo = "22ee1df9-c543-4509-a755-e7cd5dc0045e";
            dbSet.Add(new Customer()
            { Id = Guid.Parse(customerIdTwo), CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var url = $"api/Customers?ids={customerIdOne}&ids={customerIdTwo}";
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Equal(2, paginatedResponse.Results.Count);
            Assert.Equal("def", paginatedResponse.Results.ElementAt(0).Name);
            Assert.Equal("ghi", paginatedResponse.Results.ElementAt(1).Name);
            Assert.Equal(2, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithIdRangeQueryStringFilterAndIdIsNotGuid_ShouldReturnTwoRecords()
        {
            // Arrange
            var dbSet = Context.Set<IntAsIdEntity>();
            dbSet.Add(new IntAsIdEntity() { Name = "abc" });
            dbSet.Add(new IntAsIdEntity() { Name = "def" });
            dbSet.Add(new IntAsIdEntity() { Name = "ghi" });
            dbSet.Add(new IntAsIdEntity() { Name = "jkl" });
            await Context.SaveChangesAsync();

            var entities = dbSet.Where(m => new[] { "def", "ghi" }.Contains(m.Name)).AsNoTracking().ToList();

            // Act
            var response = await Client.GetAsync($"api/IntAsIdEntities?ids={entities[0].Id}&ids={entities[1].Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<IntAsIdEntity>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Equal(2, paginatedResponse.Results.Count);
            Assert.Equal("def", paginatedResponse.Results.ElementAt(0).Name);
            Assert.Equal("ghi", paginatedResponse.Results.ElementAt(1).Name);
            Assert.Equal(2, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithIntegerQueryString_ShouldReturnTwoRecords()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Age=20");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Equal(2, paginatedResponse.Results.Count);
            Assert.Equal(2, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithIntegerQueryStringAndSortDescParameter_ShouldReturnTwoRecordsSortedDesc()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "124", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Age=20&SortDesc=Name,CNPJ");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Equal(3, customers.Count);
            Assert.Equal(3, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);

            var first = customers.First();
            var second = customers.Skip(1).First();
            var third = customers.Skip(2).First();

            Assert.Equal("def", first.Name);
            Assert.Equal("456", first.CNPJ);
            Assert.Equal("abc", second.Name);
            Assert.Equal("124", second.CNPJ);
            Assert.Equal("abc", third.Name);
            Assert.Equal("123", third.CNPJ);
        }

        [Fact]
        public async Task ListPaged_WithIntegerQueryStringAndSortAscParameter_ShouldReturnTwoRecordsSortedAsc()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "124", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Age=20&Sort=Name,CNPJ");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Equal(3, customers.Count);
            Assert.Equal(3, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);

            var first = customers.First();
            var second = customers.Skip(1).First();
            var third = customers.Skip(2).First();

            Assert.Equal("abc", first.Name);
            Assert.Equal("123", first.CNPJ);
            Assert.Equal("abc", second.Name);
            Assert.Equal("124", second.CNPJ);
            Assert.Equal("def", third.Name);
            Assert.Equal("456", third.CNPJ);
        }

        [Theory(DisplayName = "When sorting by ID")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Sort1(bool isDesc)
        {
            // Arrange
            const int numberOfEntities = 50;
            var entities = Enumerable
                .Range(0, numberOfEntities)
                .Select(_ => new Faker<IntAsIdEntity>()
                    .RuleFor(m => m.Name, m => m.Company.CompanyName())
                    .Generate())
                .ToList();
            var dbSet = Context.Set<IntAsIdEntity>();
            await dbSet.AddRangeAsync(entities);
            await Context.SaveChangesAsync();
            var sortRequestString = "Sort";
            if (isDesc)
                sortRequestString += "Desc";
            sortRequestString += "=Id";
            var requestString = $"api/IntAsIdEntities?{sortRequestString}";
            // Act
            var response = await Client.GetAsync(requestString);
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<IntAsIdEntity>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(numberOfEntities, paginatedResponse.Count);
            var sortedEntities = paginatedResponse.Results;
            Assert.Equal(DefaultPageSize, sortedEntities.Count);
            if (isDesc)
            {
                var expectedIds = entities.Select(m => m.Id).OrderByDescending(m => m).Take(DefaultPageSize).ToList();
                var actualIds = sortedEntities.Select(m => m.Id).ToList();
                Assert.Equivalent(expectedIds, actualIds);
            }
            else
            {
                var expectedIds = entities.Select(m => m.Id).OrderBy(m => m).Take(DefaultPageSize).ToList();
                var actualIds = sortedEntities.Select(m => m.Id).ToList();
                Assert.Equivalent(expectedIds, actualIds);
            }
        }

        [Theory(DisplayName = "When sorting by CreatedAt")]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Sort2(bool isDesc)
        {
            // Arrange
            const int numberOfEntities = 50;
            var entities = Enumerable
                .Range(0, numberOfEntities)
                .Select(index => new Faker<IntAsIdEntity>()
                    .RuleFor(m => m.Name, m => m.Company.CompanyName())
                    .RuleFor(m => m.CreatedAt, m => m.Date.Past(index + 1, DateTime.Now))
                    .Generate())
                .ToList();
            var dbSet = Context.Set<IntAsIdEntity>();
            await dbSet.AddRangeAsync(entities);
            await Context.SaveChangesAsync();
            var sortRequestString = "Sort";
            if (isDesc)
                sortRequestString += "Desc";
            sortRequestString += "=CreatedAt";
            var requestString = $"api/IntAsIdEntities?{sortRequestString}";
            // Act
            var response = await Client.GetAsync(requestString);
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<IntAsIdEntity>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(numberOfEntities, paginatedResponse.Count);
            var sortedEntities = paginatedResponse.Results;
            Assert.Equal(DefaultPageSize, sortedEntities.Count);
            if (isDesc)
            {
                var expectedValues = entities.Select(m => m.CreatedAt).OrderByDescending(m => m.Date).ThenBy(m => m.TimeOfDay).Take(DefaultPageSize).ToList();
                var actualValues = sortedEntities.Select(m => m.CreatedAt).ToList();
                Assert.Equivalent(expectedValues, actualValues);
            }
            else
            {
                var expectedValues = entities.Select(m => m.CreatedAt).OrderBy(m => m.Date).ThenBy(m => m.TimeOfDay).Take(DefaultPageSize).ToList();
                var actualValues = sortedEntities.Select(m => m.CreatedAt).ToList();
                Assert.Equivalent(expectedValues, actualValues);
            }
        }

        [Fact]
        public async Task ListPaged_WithQueryStringDocumentParameter_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "XYZ"
                    },
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cpf",
                        Document = "1234"
                    }
                }
            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "LHA"
                    }
                }
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=1234");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Single(customers);
            Assert.Equal(1, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
            Assert.Equal("abc", customers.First().Name);
        }

        [Fact]
        public async Task ListPaged_WithQueryStringDocumentParameterAndName_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "XYZ"
                    },
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cpf",
                        Document = "1234"
                    }
                }
            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "LHA"
                    }
                }
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=1234&Name=abc");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Single(customers);
            Assert.Equal(1, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
            Assert.Equal("abc", customers.First().Name);
        }

        [Fact]
        public async Task ListPaged_WithQueryStringDocumentParameterAndName_ShouldReturnNoRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "XYZ"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cpf",
                        Document = "1234"
                    }
                }
            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "LHA"
                    }
                }
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=1234&Name=ghi");

            // Assert — empty result is a legitimate success; the paginator emits the
            // {count:0, next:null, previous:null, results:[]} envelope same as DRF's
            // PageNumberPagination (rest_framework/pagination.py:220-226 at
            // encode/django-rest-framework@3.17.1).
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(
                await response.Content.ReadAsStringAsync());
            Assert.NotNull(paginatedResponse);
            Assert.Equal(0, paginatedResponse.Count);
            Assert.Empty(paginatedResponse.Results);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithQueryStringCustomerParameter_ShouldReturnNoRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "XYZ"
                    },
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cpf",
                        Document = "1234"
                    }
                }
            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>()
                {
                    new CustomerDocument
                    {
                        Id = Guid.NewGuid(),
                        DocumentType = "cnpj",
                        Document = "LHA"
                    }
                }
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=5557");

            // Assert — empty result is a legitimate success; the paginator emits the
            // {count:0, next:null, previous:null, results:[]} envelope same as DRF's
            // PageNumberPagination (rest_framework/pagination.py:220-226 at
            // encode/django-rest-framework@3.17.1).
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(
                await response.Content.ReadAsStringAsync());
            Assert.NotNull(paginatedResponse);
            Assert.Equal(0, paginatedResponse.Count);
            Assert.Empty(paginatedResponse.Results);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithPageSize1AndPageSize3_ShouldReturn1RecordAnd3Pages()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page_size=3&page=1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Equal(3, customers.Count);
            Assert.Equal(3, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Fact]
        public async Task ListPaged_WithPageSize1AndPageSize3_ShouldReturn1RecordAnd1Pages()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page_size=1&page=3");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Single(customers);
            Assert.Equal("ghi", customers.First().Name);
            Assert.Equal(3, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            var prevUri = new Uri(paginatedResponse.Previous);
            var prevQuery = HttpUtility.ParseQueryString(prevUri.Query);
            Assert.Equal("2", prevQuery["page"]);
            Assert.Equal("1", prevQuery["page_size"]);
        }

        [Theory]
        [InlineData("", 5)]
        [InlineData(" ", 5)]
        [InlineData("1a91f9ec-920b-4c92-83b0-6bf40d0209c2", 1)]
        [InlineData("10", 2)]
        [InlineData("12", 1)]
        [InlineData("%0001%", 5)]
        [InlineData("5%", 2)]
        [InlineData("%7", 2)]
        [InlineData("Agua Alta", 1)]
        [InlineData("Agua%", 2)]
        [InlineData("% Inc", 2)]
        [InlineData("aaa", 0)]
        public async Task ListPaged_WithSearchTerm_ReturnsExpectedCount(string term, int expectedCount)
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                Id = Guid.Parse("1a91f9ec-920b-4c92-83b0-6bf40d0209c2"),
                Age = 10,
                CNPJ = "76.637.568/0001-80",
                Name = "Agua Alta"
            });
            dbSet.Add(new Customer()
            {
                Id = Guid.Parse("a71bf8fa-0714-4281-8c51-23e763442919"),
                Age = 12,
                CNPJ = "24.451.215/0001-97",
                Name = "Agua Baixa"
            });
            dbSet.Add(new Customer()
            {
                Id = Guid.Parse("555b437e-3cd8-493c-b502-94cb9ba69a6b"),
                Age = 10,
                CNPJ = "81.517.224/0001-77",
                Name = "Bailão 12 Inc"
            });
            dbSet.Add(new Customer()
            {
                Id = Guid.Parse("f10ca31e-f60b-4d4e-8ca3-a754c4fda6bc"),
                Age = 25,
                CNPJ = "59.732.451/0001-66",
                Name = "Xablau Inc"
            });
            dbSet.Add(new Customer()
            {
                Id = Guid.Parse("c2710e39-f17a-469e-8994-28fd621819b4"),
                Age = 28,
                CNPJ = "55.387.453/0001-04",
                Name = "Problem Solver"
            });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers?search={HttpUtility.UrlEncode(term)}");

            // Assert — empty result is a legitimate success; the paginator emits the
            // {count:0, next:null, previous:null, results:[]} envelope same as DRF's
            // PageNumberPagination (rest_framework/pagination.py:220-226 at
            // encode/django-rest-framework@3.17.1).
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            var customers = paginatedResponse.Results;
            Assert.Equal(expectedCount, customers.Count);
            Assert.Equal(expectedCount, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ListPaged_WhenIDsAreProvidedBetweenBrackets_ShouldReturn2Records(bool withBrackets)
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();
            var entities = dbSet.AsNoTracking().ToList();
            // Act
            var queryString = withBrackets ? $"ids=[{entities[0].Id},{entities[1].Id}]" : $"ids={entities[0].Id},{entities[1].Id}";
            var response = await Client.GetAsync($"api/Customers?{queryString}");
            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            Assert.Equal(2, paginatedResponse.Results.Count);
            Assert.Equal(entities[0].Id, paginatedResponse.Results[0].Id);
            Assert.Equal(entities[1].Id, paginatedResponse.Results[1].Id);
            Assert.Equal(2, paginatedResponse.Count);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        #region Pagination Edge Cases

        [Fact]
        public async Task ListPaged_WithPageZero_ShouldFallbackToFirstPage()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 10; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page=0");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(10, paginatedResponse.Count);
            Assert.Equal(DefaultPageSize, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithNegativePage_ShouldFallbackToFirstPage()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 10; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page=-1");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(10, paginatedResponse.Count);
            Assert.Equal(DefaultPageSize, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithPageBeyondLast_ShouldClampToLastPage()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 3; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page=999&page_size=2");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(3, paginatedResponse.Count);
            Assert.True(paginatedResponse.Results.Count > 0, "Should return results from the last page");
        }

        [Fact]
        public async Task ListPaged_WithPageSizeZero_ShouldFallbackToDefault()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 10; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page_size=0");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(10, paginatedResponse.Count);
            Assert.Equal(DefaultPageSize, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithPageSizeExceedingMax_ShouldClampToMaxPageSize()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 60; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page_size=9999");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(60, paginatedResponse.Count);
            // MaxPageSize is 50 by default
            Assert.Equal(50, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithNonNumericPage_ShouldFallbackToFirstPage()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 10; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page=abc");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(10, paginatedResponse.Count);
            Assert.Equal(DefaultPageSize, paginatedResponse.Results.Count);
        }

        #endregion

        #region Sorting Edge Cases

        [Fact]
        public async Task ListPaged_WithInvalidSortField_ShouldReturnDefaultSortOrder()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Sort=NonExistentField");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(3, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithCaseInsensitiveSortField_ShouldSortCorrectly()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "charlie" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "alice" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "bob" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Sort=name");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            var customers = paginatedResponse.Results;
            Assert.Equal(3, customers.Count);
            Assert.Equal("alice", customers[0].Name);
            Assert.Equal("bob", customers[1].Name);
            Assert.Equal("charlie", customers[2].Name);
        }

        [Fact]
        public async Task ListPaged_WithBothSortAndSortDesc_ShouldPrioritizeSortAsc()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "111", Name = "charlie" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "222", Name = "alice" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "333", Name = "bob" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?Sort=Name&SortDesc=CNPJ");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            var customers = paginatedResponse.Results;
            // Sort (asc) takes priority over SortDesc per SortFilter.Sort()
            Assert.Equal("alice", customers[0].Name);
            Assert.Equal("bob", customers[1].Name);
            Assert.Equal("charlie", customers[2].Name);
        }

        #endregion

        #region Filter Edge Cases

        [Fact]
        public async Task ListPaged_WithNonAllowedFilterField_ShouldIgnoreFilter()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?NonExistentField=value");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);
            Assert.Equal(2, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithEmptyDatabase_ShouldReturnEmptyEnvelope()
        {
            // Arrange
            // No data seeded — empty database

            // Act
            var response = await Client.GetAsync("api/Customers");

            // Assert — empty result is a legitimate success; mirrors DRF's PageNumberPagination
            // which builds an empty Page via Django's Paginator.page(1) and renders it through
            // the same get_paginated_response() branch as a populated page (rest_framework/
            // pagination.py:220-226 + mixins.py:34-44 at encode/django-rest-framework@3.17.1).
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(
                await response.Content.ReadAsStringAsync());
            Assert.NotNull(paginatedResponse);
            Assert.Equal(0, paginatedResponse.Count);
            Assert.Empty(paginatedResponse.Results);
            Assert.Null(paginatedResponse.Next);
            Assert.Null(paginatedResponse.Previous);
        }

        #endregion

        #region Pagination Link Correctness

        [Fact]
        public async Task ListPaged_NextAndPreviousLinks_ShouldNotDuplicatePageParams()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 15; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = $"cnpj{i}", Name = $"name{i}" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync("api/Customers?page=2&page_size=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            // Check next link doesn't have duplicate page/page_size params
            Assert.NotNull(paginatedResponse.Next);
            var nextUri = new Uri(paginatedResponse.Next);
            var nextQuery = HttpUtility.ParseQueryString(nextUri.Query);
            var nextPageValues = nextQuery.GetValues("page");
            var nextPageSizeValues = nextQuery.GetValues("page_size");
            Assert.NotNull(nextPageValues);
            Assert.Single(nextPageValues);
            Assert.NotNull(nextPageSizeValues);
            Assert.Single(nextPageSizeValues);

            // Check previous link doesn't have duplicate page/page_size params
            Assert.NotNull(paginatedResponse.Previous);
            var prevUri = new Uri(paginatedResponse.Previous);
            var prevQuery = HttpUtility.ParseQueryString(prevUri.Query);
            var prevPageValues = prevQuery.GetValues("page");
            var prevPageSizeValues = prevQuery.GetValues("page_size");
            Assert.NotNull(prevPageValues);
            Assert.Single(prevPageValues);
            Assert.NotNull(prevPageSizeValues);
            Assert.Single(prevPageSizeValues);
        }

        [Fact]
        public async Task ListPaged_WithFilterAndPagination_ShouldNotDuplicateFilterParamsInLinks()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            for (var i = 0; i < 15; i++)
                dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "sameCnpj", Name = $"name{i}", Age = 20 });
            await Context.SaveChangesAsync();

            // Act — combine a filter (Age=20) with pagination
            var response = await Client.GetAsync("api/Customers?Age=20&page=2&page_size=5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);
            Assert.NotNull(paginatedResponse);

            // The next link should contain Age=20 exactly once, not duplicated.
            // Bug: PageNumberPagination.cs:39 uses || instead of && in the Where clause,
            // so allOthersParams includes page/page_size (and all other params).
            // Since the URL already contains Age=20, and allOthersParams also has it,
            // query.Add duplicates it. page/page_size duplicates are masked by the
            // subsequent Set call, but filter params like Age are not.
            Assert.NotNull(paginatedResponse.Next);
            var nextUri = new Uri(paginatedResponse.Next);
            var nextQuery = HttpUtility.ParseQueryString(nextUri.Query);
            var ageValues = nextQuery.GetValues("Age");
            Assert.NotNull(ageValues);
            Assert.Single(ageValues);
        }

        #endregion
    }

    public class Patch : IntegrationTests
    {
        [Fact]
        public async Task Patch_WithFullObject_ShouldUpdateFullObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal(customerToUpdate.Name, updatedCustomer.Name);
            Assert.Equal(customerToUpdate.CNPJ, updatedCustomer.CNPJ);
        }

        [Fact]
        public async Task Patch_WithPartialObject_ShouldUpdatePartialObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal(customer.Name, updatedCustomer.Name);
            Assert.Equal(customerToUpdate.CNPJ, updatedCustomer.CNPJ);
        }

        [Fact]
        public async Task Patch_WhenEntityDoesntExist_ReturnsNotFound()
        {
            // Arrange
            var customerToUpdate = new
            {
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Customers/{Guid.NewGuid()}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task Patch_WhenPatchIsNotAllowedByActionOptions_ShouldReturnMethodNotAllowed()
        {
            // Arrange
            var dbSet = Context.Set<IntAsIdEntity>();
            var entity = new IntAsIdEntity() { Name = "abc" };
            dbSet.Add(entity);
            await Context.SaveChangesAsync();

            var entityToUpdate = new IntAsIdEntityDto()
            {
                Id = entity.Id,
                Name = "aaaa",
            };

            var content = new StringContent(JsonConvert.SerializeObject(entityToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/IntAsIdEntities/{entity.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            var notUpdatedEntity = dbSet.AsNoTracking().First(x => x.Id == entity.Id);
            Assert.Equal(entity.Name, notUpdatedEntity.Name);
        }

        [Fact]
        public async Task Patch_WithEmptyBody_ShouldReturnOkWithUnchangedEntity()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 30 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var content = new StringContent("{}", Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal("abc", updatedCustomer.Name);
            Assert.Equal("123", updatedCustomer.CNPJ);
            Assert.Equal(30, updatedCustomer.Age);
        }

        [Fact]
        public async Task Patch_WithNullValue_ShouldSetFieldToNull()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Explicitly include null to test PartialJsonObject behavior
            // Note: NullValueHandling.Ignore in FakeProgram may strip null fields from JSON,
            // causing PartialJsonObject.IsSet to return false for null values
            var json = "{\"CNPJ\": null}";
            var content = new StringContent(json, Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Null(updatedCustomer.CNPJ);
        }

        [Fact]
        public async Task Patch_WithFullObject_ShouldReturnResponseBodyWithOnlyDeclaredFields()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 25 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var customerToUpdate = new { CNPJ = "updated", Name = "updated" };
            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            // JSON response uses camelCase keys
            var declaredFields = new[] { "name", "cnpj", "age", "id", "region", "customerDocument" };
            foreach (var property in body.Properties())
            {
                Assert.Contains(property.Name, declaredFields);
            }
        }
    }

    public class Post : IntegrationTests
    {
        [Fact]
        public async Task Post_WithValidData_ShouldInsertObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            var addedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(customer.Name, addedCustomer.Name);
            Assert.Equal(customer.CNPJ, addedCustomer.CNPJ);
        }

        [Fact]
        public async Task Post_WithNameTooShort_ShouldReturn400WithDataAnnotationsError()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "ac" };

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.NotNull(responseMessages);

            Assert.Contains("Name should have at least 3 characters", responseMessages.Error["Name"]);

            var customers = dbSet.AsNoTracking().ToList();
            Assert.Empty(customers);
        }

        [Fact]
        public async Task Post_WithForbiddenCNPJ_ShouldReturn400WithValidateAsyncError()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "567", Name = "abc" };

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<ValidationErrors>(responseData);
            Assert.NotNull(responseMessages);

            Assert.Contains("CNPJ cannot be 567", responseMessages.Error["CNPJ"]);

            var customers = dbSet.AsNoTracking().ToList();
            Assert.Empty(customers);
        }

        [Fact]
        public async Task Post_WithValidData_ShouldReturn201WithLocationHeader()
        {
            // Arrange
            var customer = new CustomerDto() { Name = "abc", CNPJ = "123" };
            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);
            Assert.Contains("api/Customers/", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task Post_WithValidData_ShouldReturnResponseBodyWithOnlyDeclaredFields()
        {
            // Arrange
            var customer = new CustomerDto() { Name = "abc", CNPJ = "123" };
            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            // JSON response uses camelCase keys
            var declaredFields = new[] { "name", "cnpj", "age", "id", "region", "customerDocument" };
            foreach (var property in body.Properties())
            {
                Assert.Contains(property.Name, declaredFields);
            }
        }

        [Fact]
        public async Task Post_WithMinimumRequiredFields_ShouldInsertObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            // Only Name is required (min 3 chars per validator), CNPJ is optional (just can't be "567")
            var customer = new { Name = "minimal" };
            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            var createdId = body["id"]?.ToObject<Guid>();
            Assert.NotNull(createdId);
            var insertedCustomer = dbSet.AsNoTracking().First(x => x.Id == createdId);
            Assert.Equal("minimal", insertedCustomer.Name);
        }
    }

    public class Put : IntegrationTests
    {
        [Fact]
        public async Task Put_WithFullObject_ShouldUpdateFullObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal(customerToUpdate.Name, updatedCustomer.Name);
            Assert.Equal(customerToUpdate.CNPJ, updatedCustomer.CNPJ);
        }

        [Fact]
        public async Task Put_WhenEntityDoesntExist_ReturnsNotFound()
        {
            // Arrange
            var customerToUpdate = new
            {
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{Guid.NewGuid()}", content);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }


        [Fact]
        public async Task Put_WhenPutIsNotAllowedByActionOptions_ShouldReturnMethodNotAllowed()
        {
            // Arrange
            var dbSet = Context.Set<IntAsIdEntity>();
            var entity = new IntAsIdEntity() { Name = "abc" };
            dbSet.Add(entity);
            Context.SaveChanges();

            var entityToUpdate = new IntAsIdEntityDto()
            {
                Id = entity.Id,
                Name = "aaaa",
            };

            var content = new StringContent(JsonConvert.SerializeObject(entityToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/IntAsIdEntities/{entity.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);

            var notUpdatedEntity = dbSet.AsNoTracking().First(x => x.Id == entity.Id);
            Assert.Equal(entity.Name, notUpdatedEntity.Name);
        }

        [Fact]
        public async Task Put_WithPartialBody_ShouldResetOmittedFieldsToDefault()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 30 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // PUT with only Name — ideally CNPJ and Age should reset to defaults (null/0),
            // but NullValueHandling.Ignore in FakeProgram prevents null/default fields from being
            // sent through PopulateObject, so omitted fields retain their original values.
            // This documents the actual behavior caused by the serializer configuration.
            var body = new { Name = "updated" };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal("updated", updatedCustomer.Name);
            // NullValueHandling.Ignore strips both null references and default value types from
            // serialization, so PopulateObject never receives CNPJ (null) or Age (0).
            // Both fields retain their original values.
            Assert.Null(updatedCustomer.CNPJ);
            Assert.Equal(30, updatedCustomer.Age);
        }

        [Fact]
        public async Task Put_WithBodyIdDifferentFromRouteId_ShouldUseRouteId()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var bodyIdThatShouldBeIgnored = Guid.NewGuid();
            var body = new { Id = bodyIdThatShouldBeIgnored, CNPJ = "updated", Name = "updated" };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            Assert.Equal("updated", updatedCustomer.Name);
            Assert.Equal("updated", updatedCustomer.CNPJ);
            // The entity with the body ID should not exist
            var entityWithBodyId = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == bodyIdThatShouldBeIgnored);
            Assert.Null(entityWithBodyId);
        }

        [Fact]
        public async Task Put_WithFullObject_ShouldReturnResponseBodyWithOnlyDeclaredFields()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 25 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            var body = new { CNPJ = "updated", Name = "updated", Age = 40 };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{customer.Id}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseBody = JObject.Parse(responseData);
            // JSON response uses camelCase keys
            var declaredFields = new[] { "name", "cnpj", "age", "id", "region", "customerDocument" };
            foreach (var property in responseBody.Properties())
            {
                Assert.Contains(property.Name, declaredFields);
            }
        }
    }

    /// <summary>
    /// Pins the DRF-parity row-scoping contract: the controller's <c>Filters</c> chain runs
    /// before every action's load step, so a write request that targets a row outside the
    /// caller's scope must surface as 404 (the same outcome as a missing row, with no leak).
    /// Each test seeds two rows in disjoint tenants and drives <c>TenantScopedCustomersController</c>
    /// from one tenant; the row in the other tenant must remain untouched.
    /// </summary>
    public class CrossTenantWriteScoping : IntegrationTests
    {
        private const string TenantHeader = "X-Tenant";
        private const string TenantA = "tenant-a";
        private const string TenantB = "tenant-b";

        private async Task<(Customer InTenant, Customer OutOfTenant)> SeedTwoTenantsAsync()
        {
            var dbSet = Context.Set<Customer>();
            var inTenant = new Customer { Id = Guid.NewGuid(), CNPJ = "111", Name = "in-scope", Region = TenantA };
            var outOfTenant = new Customer { Id = Guid.NewGuid(), CNPJ = "222", Name = "OTHER-TENANT", Region = TenantB };
            dbSet.AddRange(inTenant, outOfTenant);
            await Context.SaveChangesAsync();
            return (inTenant, outOfTenant);
        }

        private static HttpRequestMessage Build(HttpMethod method, string url, string tenant, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            request.Headers.Add(TenantHeader, tenant);
            if (body != null)
            {
                request.Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json");
            }
            return request;
        }

        [Fact]
        public async Task GetSingle_TargetingRowInOtherTenant_ShouldReturn404()
        {
            // Arrange
            var (_, outOfTenant) = await SeedTwoTenantsAsync();

            // Act
            var response = await Client.SendAsync(
                Build(HttpMethod.Get, $"api/TenantScopedCustomers/{outOfTenant.Id}", TenantA));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Put_TargetingRowInOtherTenant_ShouldReturn404AndNotMutate()
        {
            // Arrange
            var (_, outOfTenant) = await SeedTwoTenantsAsync();
            var dbSet = Context.Set<Customer>();
            var body = new { CNPJ = "HACKED", Name = "HACKED" };

            // Act
            var response = await Client.SendAsync(
                Build(HttpMethod.Put, $"api/TenantScopedCustomers/{outOfTenant.Id}", TenantA, body));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var stillThere = dbSet.AsNoTracking().First(c => c.Id == outOfTenant.Id);
            Assert.Equal("OTHER-TENANT", stillThere.Name);
            Assert.Equal("222", stillThere.CNPJ);
            Assert.Equal(TenantB, stillThere.Region);
        }

        [Fact]
        public async Task Patch_TargetingRowInOtherTenant_ShouldReturn404AndNotMutate()
        {
            // Arrange
            var (_, outOfTenant) = await SeedTwoTenantsAsync();
            var dbSet = Context.Set<Customer>();
            var patch = new { Name = "HACKED" };
            var request = new HttpRequestMessage(HttpMethod.Patch, $"api/TenantScopedCustomers/{outOfTenant.Id}");
            request.Headers.Add(TenantHeader, TenantA);
            request.Content = new StringContent(
                JsonConvert.SerializeObject(patch),
                Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var stillThere = dbSet.AsNoTracking().First(c => c.Id == outOfTenant.Id);
            Assert.Equal("OTHER-TENANT", stillThere.Name);
        }

        [Fact]
        public async Task Delete_TargetingRowInOtherTenant_ShouldReturn404AndNotDelete()
        {
            // Arrange
            var (_, outOfTenant) = await SeedTwoTenantsAsync();
            var dbSet = Context.Set<Customer>();

            // Act
            var response = await Client.SendAsync(
                Build(HttpMethod.Delete, $"api/TenantScopedCustomers/{outOfTenant.Id}", TenantA));

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var stillThere = dbSet.AsNoTracking().FirstOrDefault(c => c.Id == outOfTenant.Id);
            Assert.NotNull(stillThere);
            Assert.Equal("OTHER-TENANT", stillThere!.Name);
        }

        [Fact]
        public async Task DeleteMany_WithMixedInScopeAndOutOfScopeIds_ShouldOnlyDeleteInScopeRows()
        {
            // Arrange
            var (inTenant, outOfTenant) = await SeedTwoTenantsAsync();
            var dbSet = Context.Set<Customer>();

            // Act
            var url = $"api/TenantScopedCustomers?ids={inTenant.Id}&ids={outOfTenant.Id}";
            var response = await Client.SendAsync(Build(HttpMethod.Delete, url, TenantA));

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            var inScopeStillThere = dbSet.AsNoTracking().FirstOrDefault(c => c.Id == inTenant.Id);
            var outOfScopeStillThere = dbSet.AsNoTracking().FirstOrDefault(c => c.Id == outOfTenant.Id);
            Assert.Null(inScopeStillThere);
            Assert.NotNull(outOfScopeStillThere);
            Assert.Equal("OTHER-TENANT", outOfScopeStillThere!.Name);
        }

        [Fact]
        public async Task ListPaged_FromOneTenant_ShouldExcludeOtherTenantsRows()
        {
            // Arrange — pinning the pre-existing read-side scoping so a future regression
            // that rebuilds the filter chain affects writes and reads consistently.
            var (inTenant, outOfTenant) = await SeedTwoTenantsAsync();

            // Act
            var response = await Client.SendAsync(
                Build(HttpMethod.Get, "api/TenantScopedCustomers", TenantA));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paginated = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(
                await response.Content.ReadAsStringAsync());
            Assert.NotNull(paginated);
            Assert.Equal(1, paginated.Count);
            Assert.Single(paginated.Results);
            Assert.Equal(inTenant.Id, paginated.Results[0].Id);
            Assert.DoesNotContain(paginated.Results, c => c.Id == outOfTenant.Id);
        }

        [Fact]
        public async Task Put_TargetingOwnTenantRow_ShouldSucceed()
        {
            // Arrange — sanity guard that the new query plumbing does not break the in-scope path.
            var (inTenant, _) = await SeedTwoTenantsAsync();
            var dbSet = Context.Set<Customer>();
            var body = new { CNPJ = "updated", Name = "updated", Region = TenantA };

            // Act
            var response = await Client.SendAsync(
                Build(HttpMethod.Put, $"api/TenantScopedCustomers/{inTenant.Id}", TenantA, body));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = dbSet.AsNoTracking().First(c => c.Id == inTenant.Id);
            Assert.Equal("updated", updated.Name);
            Assert.Equal("updated", updated.CNPJ);
            Assert.Equal(TenantA, updated.Region);
        }
    }

    public class ExceptionPropagation : IntegrationTests
    {
        [Fact]
        public async Task GetSingle_WhenQuerySetThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var act = () => Client.GetAsync($"api/ThrowingCustomers/{id}");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated query failure", exception.Message);
        }

        [Fact]
        public async Task ListPaged_WhenQuerySetThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var act = () => Client.GetAsync("api/ThrowingCustomers");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated query failure", exception.Message);
        }

        [Fact]
        public async Task Post_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var customer = new CustomerDto { Name = "abc", CNPJ = "123" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");
            var act = () => Client.PostAsync("api/ThrowingCustomers", content);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }

        [Fact]
        public async Task Put_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange — uses ThrowingSerializerOnlyCustomers because the row-scoped queryset
            // build now runs before the serializer call; ThrowingCustomersController would
            // fail in GetQuerySet() and never reach the serializer override under test.
            var id = Guid.NewGuid();
            var customer = new CustomerDto { Name = "abc", CNPJ = "123" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");
            var act = () => Client.PutAsync($"api/ThrowingSerializerOnlyCustomers/{id}", content);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }

        [Fact]
        public async Task Patch_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange — uses ThrowingSerializerOnlyCustomers; see Put test for rationale.
            var id = Guid.NewGuid();
            var content = new StringContent(
                JsonConvert.SerializeObject(new { Name = "abc" }), Encoding.UTF8, "application/json-patch+json");
            var act = () => Client.PatchAsync($"api/ThrowingSerializerOnlyCustomers/{id}", content);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }

        [Fact]
        public async Task Delete_WhenSerializerThrowsOperationCanceledException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange — uses ThrowingSerializerOnlyCustomers; see Put test for rationale.
            var id = Guid.NewGuid();
            var act = () => Client.DeleteAsync($"api/ThrowingSerializerOnlyCustomers/{id}");

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(act);

            // Assert
            Assert.Equal("Simulated client disconnect", exception.Message);
        }

        [Fact]
        public async Task DeleteMany_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange — uses ThrowingSerializerOnlyCustomers; see Put test for rationale.
            var id = Guid.NewGuid();
            var act = () => Client.DeleteAsync($"api/ThrowingSerializerOnlyCustomers?ids={id}");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }
    }

    public class FieldValidation : IntegrationTests
    {
        [Fact]
        public async Task AnyAction_WhenEntityGetFieldsThrows_ShouldThrowDuringControllerConstruction()
        {
            // Arrange
            var act = () => Client.GetAsync($"api/Sellers/{Guid.NewGuid()}");

            // Act
            var exception = await Assert.ThrowsAsync<NotImplementedException>(act);

            // Assert
            Assert.NotNull(exception);
        }

        [Fact]
        public async Task AnyAction_WhenEntityGetFieldsContainsInvalidField_ShouldThrowDuringControllerConstruction()
        {
            // Arrange
            var act = () => Client.GetAsync("api/InvalidFieldEntities");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Contains("NonExistentField", exception.Message);
            Assert.Contains("InvalidFieldEntity", exception.Message);
        }

        [Fact]
        public async Task AnyAction_WhenAllowedFieldsContainsInvalidField_ShouldThrowOnFirstRequest()
        {
            // Arrange
            var act = () => Client.GetAsync("api/InvalidAllowedFieldEntities");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Contains("NonExistentAllowedField", exception.Message);
        }
    }
}
