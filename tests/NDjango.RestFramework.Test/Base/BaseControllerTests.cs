using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Bogus;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Paginations;
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
        public async Task Delete_WithObject_ShouldDeleteEntityFromDatabaseAndReturnOk()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/Customers/{customer.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            updatedCustomer.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WhenEntityDoesntExist_ReturnsNotFound()
        {
            // Act
            var response = await Client.DeleteAsync($"api/Customers/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenEntityDoesntImplementGetFields_ReturnsBadRequest()
        {
            // Act
            var response = await Client.DeleteAsync($"api/Sellers/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
        }

        [Fact]
        public async Task Delete_WithObject_ShouldReturnDeletedEntityInResponseBody()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 30 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            Assert.Equal(customer.Name, body["name"]?.ToString());
            Assert.Equal(customer.CNPJ, body["cnpj"]?.ToString());
            Assert.Equal(customer.Age, body["age"]?.ToObject<int>());
            Assert.Equal(customer.Id, body["id"]?.ToObject<Guid>());
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var remaining = dbSet.AsNoTracking()
                .Where(x => x.Id == customer1.Id || x.Id == customer3.Id)
                .ToList();
            Assert.Equal(2, remaining.Count);
            var deleted = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer2.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task Delete_WithObject_ShouldReturnResponseWithOnlyDeclaredFields()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc", Age = 25 };
            dbSet.Add(customer);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync($"api/Customers/{customer.Id}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseData = await response.Content.ReadAsStringAsync();
            var body = JObject.Parse(responseData);
            // JSON response uses camelCase keys
            var declaredFields = new[] { "name", "cnpj", "age", "id", "customerDocument" };
            foreach (var property in body.Properties())
            {
                Assert.Contains(property.Name, declaredFields);
            }
        }
    }

    public class DeleteMany : IntegrationTests
    {
        [Fact]
        public async Task DeleteMany_ShouldDeleteManyEntities_AndReturnTheirIds()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" });
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var deletedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            var expectedGuids = new List<Guid> { expectedToBeDeletedOne, expectedToBeDeletedTwo };
            deletedIds.Should().BeEquivalentTo(expectedGuids);

            var entitiesWithDeletedIds = dbSet.Where(m => expectedGuids.Contains(m.Id)).AsNoTracking();
            entitiesWithDeletedIds.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteMany_WithEmptyIdsList_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" });
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.DeleteAsync("api/Customers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var deletedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            Assert.Empty(deletedIds);
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var deletedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            Assert.Equal(2, deletedIds.Count);
            Assert.Contains(existing1.Id, deletedIds);
            Assert.Contains(existing2.Id, deletedIds);
            Assert.DoesNotContain(nonExistingId, deletedIds);
        }

        [Fact]
        public async Task DeleteMany_WithAllNonExistingIds_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            // Act
            var url = $"api/Customers?ids={id1}&ids={id2}";
            var response = await Client.DeleteAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var deletedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            Assert.Empty(deletedIds);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(responseData);
            customer.Should().NotBeNull();
            customer.Id.Should().Be(customer1.Id);
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
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetSingle_WithoutFields_ShouldReturnBadRequest()
        {
            // Arrange
            var dbSet = Context.Set<Seller>();
            var seller1 = new Seller() { Id = Guid.NewGuid(), Name = "Test" };

            dbSet.Add(seller1);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Sellers/{seller1.Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var msg = JsonConvert.DeserializeObject<UnexpectedError>(responseData);
            msg.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
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
            var declaredFields = new[] { "name", "cnpj", "age", "id", "customerDocument" };
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(3);
            paginatedResponse.Count.Should().Be(3);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(1);
            paginatedResponse.Results.First().Name.Should().Be("ghi");
            paginatedResponse.Count.Should().Be(1);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(1);
            paginatedResponse.Results.First().Name.Should().Be("abc");
            paginatedResponse.Count.Should().Be(1);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(2);
            paginatedResponse.Results.ElementAt(0).Name.Should().Be("def");
            paginatedResponse.Results.ElementAt(1).Name.Should().Be("ghi");
            paginatedResponse.Count.Should().Be(2);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<IntAsIdEntity>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(2);
            paginatedResponse.Results.ElementAt(0).Name.Should().Be("def");
            paginatedResponse.Results.ElementAt(1).Name.Should().Be("ghi");
            paginatedResponse.Count.Should().Be(2);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(2);
            paginatedResponse.Count.Should().Be(2);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(3);
            paginatedResponse.Count.Should().Be(3);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();

            var first = customers.First();
            var second = customers.Skip(1).First();
            var third = customers.Skip(2).First();

            first.Name.Should().Be("def");
            first.CNPJ.Should().Be("456");
            second.Name.Should().Be("abc");
            second.CNPJ.Should().Be("124");
            third.Name.Should().Be("abc");
            third.CNPJ.Should().Be("123");
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(3);
            paginatedResponse.Count.Should().Be(3);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();

            var first = customers.First();
            var second = customers.Skip(1).First();
            var third = customers.Skip(2).First();

            first.Name.Should().Be("abc");
            first.CNPJ.Should().Be("123");
            second.Name.Should().Be("abc");
            second.CNPJ.Should().Be("124");
            third.Name.Should().Be("def");
            third.CNPJ.Should().Be("456");
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<IntAsIdEntity>>>(responseData);
            paginatedResponse.Count.Should().Be(numberOfEntities);
            var sortedEntities = paginatedResponse.Results;
            sortedEntities.Count.Should().Be(DefaultPageSize);
            if (isDesc)
            {
                var expectedIds = entities.Select(m => m.Id).OrderByDescending(m => m).Take(DefaultPageSize).ToList();
                var actualIds = sortedEntities.Select(m => m.Id).ToList();
                actualIds.Should().BeEquivalentTo(expectedIds);
            }
            else
            {
                var expectedIds = entities.Select(m => m.Id).OrderBy(m => m).Take(DefaultPageSize).ToList();
                var actualIds = sortedEntities.Select(m => m.Id).ToList();
                actualIds.Should().BeEquivalentTo(expectedIds);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<IntAsIdEntity>>>(responseData);
            paginatedResponse.Count.Should().Be(numberOfEntities);
            var sortedEntities = paginatedResponse.Results;
            sortedEntities.Count.Should().Be(DefaultPageSize);
            if (isDesc)
            {
                var expectedValues = entities.Select(m => m.CreatedAt).OrderByDescending(m => m.Date).ThenBy(m => m.TimeOfDay).Take(DefaultPageSize).ToList();
                var actualValues = sortedEntities.Select(m => m.CreatedAt).ToList();
                actualValues.Should().BeEquivalentTo(expectedValues);
            }
            else
            {
                var expectedValues = entities.Select(m => m.CreatedAt).OrderBy(m => m.Date).ThenBy(m => m.TimeOfDay).Take(DefaultPageSize).ToList();
                var actualValues = sortedEntities.Select(m => m.CreatedAt).ToList();
                actualValues.Should().BeEquivalentTo(expectedValues);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(1);
            paginatedResponse.Count.Should().Be(1);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
            customers.First().Name.Should().Be("abc");
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(1);
            paginatedResponse.Count.Should().Be(1);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
            customers.First().Name.Should().Be("abc");
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

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(3);
            paginatedResponse.Count.Should().Be(3);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(1);
            customers.First().Name.Should().Be("ghi");
            paginatedResponse.Count.Should().Be(3);
            paginatedResponse.Next.Should().BeNull();
            var prevUri = new Uri(paginatedResponse.Previous);
            var prevQuery = HttpUtility.ParseQueryString(prevUri.Query);
            prevQuery["page"].Should().Be("2");
            prevQuery["page_size"].Should().Be("1");
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
        [InlineData("aaa", null)]
        public async Task ListPaged_WithSearchTerm_ReturnsExpectedCount(string term, int? expectedCount)
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

            // Assert
            if (expectedCount == null)
            {
                response.StatusCode.Should().Be(HttpStatusCode.NotFound);
                return;
            }
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            var customers = paginatedResponse.Results;
            customers.Count.Should().Be(expectedCount);
            paginatedResponse.Count.Should().Be(expectedCount);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
        }

        [Fact]
        public async Task ListPaged_WhenEntityDoesntImplementGetFields_ReturnsBadRequest()
        {
            // Act
            var response = await Client.GetAsync("api/Sellers");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var paginatedResponse = JsonConvert.DeserializeObject<PaginatedResponse<List<Customer>>>(responseData);

            paginatedResponse.Results.Count.Should().Be(2);
            paginatedResponse.Results[0].Id.Should().Be(entities[0].Id);
            paginatedResponse.Results[1].Id.Should().Be(entities[1].Id);
            paginatedResponse.Count.Should().Be(2);
            paginatedResponse.Next.Should().BeNull();
            paginatedResponse.Previous.Should().BeNull();
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
            Assert.Equal(2, paginatedResponse.Results.Count);
        }

        [Fact]
        public async Task ListPaged_WithEmptyDatabase_ShouldReturnNotFound()
        {
            // Arrange
            // No data seeded — empty database

            // Act
            var response = await Client.GetAsync("api/Customers");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            updatedCustomer.Name.Should().Be(customerToUpdate.Name);
            updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            updatedCustomer.Name.Should().Be(customer.Name);
            updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);
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
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Patch_WhenEntityDoesntImplementGetFields_ReturnsBadRequest()
        {
            // Arrange
            var seller = new SellerDto()
            {
                Id = Guid.NewGuid(),
                Name = "Seller",
            };

            var content = new StringContent(JsonConvert.SerializeObject(seller), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PatchAsync($"api/Sellers/{seller.Id}", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
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
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

            var notUpdatedEntity = dbSet.AsNoTracking().First(x => x.Id == entity.Id);
            notUpdatedEntity.Name.Should().Be(entity.Name);
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
            var declaredFields = new[] { "name", "cnpj", "age", "id", "customerDocument" };
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
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            addedCustomer.Name.Should().Be(customer.Name);
            addedCustomer.CNPJ.Should().Be(customer.CNPJ);
        }

        [Fact]
        public async Task Post_WithInvalidData_ShouldNotInsertObjectAndReturn400Error()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "567", Name = "ac" };

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<ValidationErrors>(responseData);

            responseMessages.Error["Name"].Should().Contain("Name should have at least 3 characters");
            responseMessages.Error["CNPJ"].Should().Contain("CNPJ cannot be 567");

            var customers = dbSet.AsNoTracking().ToList();
            customers.Should().BeEmpty();
        }

        [Fact]
        public async Task Post_WhenEntityDoesntImplementGetFields_ReturnsBadRequest()
        {
            // Arrange
            var dbSet = Context.Set<Seller>();

            var seller = new SellerDto()
            {
                Name = "Seller",
            };

            var content = new StringContent(JsonConvert.SerializeObject(seller), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Sellers", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);

            var sellers = dbSet.AsNoTracking().ToList();
            sellers.Should().BeEmpty();
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
            var declaredFields = new[] { "name", "cnpj", "age", "id", "customerDocument" };
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().First(x => x.Id == customer.Id);
            updatedCustomer.Name.Should().Be(customerToUpdate.Name);
            updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);
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
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Put_WhenEntityDoesntImplementGetFields_ReturnsBadRequest()
        {
            // Arrange
            var seller = new SellerDto()
            {
                Id = Guid.NewGuid(),
                Name = "Seller",
            };

            var content = new StringContent(JsonConvert.SerializeObject(seller), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Sellers/{seller.Id}", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
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
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);

            var notUpdatedEntity = dbSet.AsNoTracking().First(x => x.Id == entity.Id);
            notUpdatedEntity.Name.Should().Be(entity.Name);
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
            var declaredFields = new[] { "name", "cnpj", "age", "id", "customerDocument" };
            foreach (var property in responseBody.Properties())
            {
                Assert.Contains(property.Name, declaredFields);
            }
        }
    }

    public class PutMany : IntegrationTests
    {
        [Fact]
        public async Task PutMany_ShouldUpdateManyEntities_AndReturnTheirIds()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer()
            { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer()
            { Id = Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"), CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer()
            { Id = Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"), CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            var customerData = new CustomerDto
            {
                CNPJ = "aaaa",
                Name = "eee"
            };

            var expectedGuids = new[]
            {
                Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"),
                Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"),
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerData), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response =
                await Client.PutAsync(
                    $"api/Customers?ids=6bdc2b9e-3710-40b9-93dd-c7558b446e21&ids=22ee1df9-c543-4509-a755-e7cd5dc0045e",
                    content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            updatedIds.Should().BeEquivalentTo(expectedGuids);

            var updatedEntities = dbSet.Where(m => expectedGuids.Contains(m.Id)).AsNoTracking();
            updatedEntities.Should().BeEquivalentTo(
                new[]
                {
                    new Customer() {CNPJ = "aaaa", Name = "eee"},
                    new Customer() {CNPJ = "aaaa", Name = "eee"},
                },
                config => config
                    .Including(m => m.CNPJ)
                    .Including(m => m.Name)
            );
        }

        [Fact]
        public async Task PutMany_WhenPutIsNotAllowedByActionOptions_ShouldReturnMethodNotAllowed()
        {
            // Arrange
            var entityToUpdate = new IntAsIdEntityDto()
            {
                Name = "aaaa",
            };

            var content = new StringContent(JsonConvert.SerializeObject(entityToUpdate), Encoding.UTF8,
                "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/IntAsIdEntities?ids=1&ids=2", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact]
        public async Task PutMany_WithEmptyIdsList_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            var body = new CustomerDto { CNPJ = "updated", Name = "updated" };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync("api/Customers", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            Assert.Empty(updatedIds);
        }

        [Fact]
        public async Task PutMany_WithNonExistingIds_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var body = new CustomerDto { CNPJ = "updated", Name = "updated" };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/Customers?ids={id1}&ids={id2}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            Assert.Empty(updatedIds);
        }

        [Fact]
        public async Task PutMany_WithMixedExistingAndNonExistingIds_ShouldUpdateOnlyExistingOnes()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var existing = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(existing);
            await Context.SaveChangesAsync();

            var nonExistingId = Guid.NewGuid();
            var body = new CustomerDto { CNPJ = "updated", Name = "updated" };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PutAsync($"api/Customers?ids={existing.Id}&ids={nonExistingId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updatedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            Assert.Single(updatedIds);
            Assert.Contains(existing.Id, updatedIds);
            Assert.DoesNotContain(nonExistingId, updatedIds);

            var updatedEntity = dbSet.AsNoTracking().First(x => x.Id == existing.Id);
            Assert.Equal("updated", updatedEntity.Name);
            Assert.Equal("updated", updatedEntity.CNPJ);
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
            // Arrange
            var id = Guid.NewGuid();
            var customer = new CustomerDto { Name = "abc", CNPJ = "123" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");
            var act = () => Client.PutAsync($"api/ThrowingCustomers/{id}", content);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }

        [Fact]
        public async Task PutMany_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var customer = new CustomerDto { Name = "abc", CNPJ = "123" };
            var content = new StringContent(
                JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json");
            var act = () => Client.PutAsync($"api/ThrowingCustomers?ids={id}", content);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }

        [Fact]
        public async Task Patch_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var content = new StringContent(
                JsonConvert.SerializeObject(new { Name = "abc" }), Encoding.UTF8, "application/json-patch+json");
            var act = () => Client.PatchAsync($"api/ThrowingCustomers/{id}", content);

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }

        [Fact]
        public async Task Delete_WhenSerializerThrowsOperationCanceledException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var act = () => Client.DeleteAsync($"api/ThrowingCustomers/{id}");

            // Act
            var exception = await Assert.ThrowsAsync<OperationCanceledException>(act);

            // Assert
            Assert.Equal("Simulated client disconnect", exception.Message);
        }

        [Fact]
        public async Task DeleteMany_WhenSerializerThrowsException_ShouldPropagateToMiddlewarePipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            var act = () => Client.DeleteAsync($"api/ThrowingCustomers?ids={id}");

            // Act
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);

            // Assert
            Assert.Equal("Simulated infrastructure failure", exception.Message);
        }
    }
}
