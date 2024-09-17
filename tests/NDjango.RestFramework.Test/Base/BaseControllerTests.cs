using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NDjango.RestFramework.Base;
using NDjango.RestFramework.Errors;
using NDjango.RestFramework.Test.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Xunit;

namespace NDjango.RestFramework.Test.Base;

public class BaseControllerTests
{
    public class Delete : IntegrationTests
    {
        private readonly WebApplicationFactory<FakeProgram> _factory = new();

        [Fact]
        public async Task Delete_WithObject_ShouldDeleteEntityFromDatabaseAndReturnOk()
        {
            // Arrange
            var client = _factory.CreateClient();
            var context = _factory.Services.GetRequiredService<AppDbContext>();
            var dbSet = context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            await context.SaveChangesAsync();

            // Act
            var response = await client.DeleteAsync($"api/Customers/{customer.Id}");

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
    }

    public class ListPaged : IntegrationTests
    {
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);

            customers.Data.Count.Should().Be(3);
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.First().Name.Should().Be("ghi");
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.First().Name.Should().Be("abc");
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(2);
            customers.Data.ElementAt(0).Name.Should().Be("def");
            customers.Data.ElementAt(1).Name.Should().Be("ghi");
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<IntAsIdEntity>>>(responseData);
            customers.Data.Count.Should().Be(2);
            customers.Data.ElementAt(0).Name.Should().Be("def");
            customers.Data.ElementAt(1).Name.Should().Be("ghi");
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(2);
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(3);

            var first = customers.Data.First();
            var second = customers.Data.Skip(1).First();
            var third = customers.Data.Skip(2).First();

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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(3);

            var first = customers.Data.First();
            var second = customers.Data.Skip(1).First();
            var third = customers.Data.Skip(2).First();

            first.Name.Should().Be("abc");
            first.CNPJ.Should().Be("123");
            second.Name.Should().Be("abc");
            second.CNPJ.Should().Be("124");
            third.Name.Should().Be("def");
            third.CNPJ.Should().Be("456");
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.First().Name.Should().Be("abc");
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
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.First().Name.Should().Be("abc");
        }

        [Fact]
        public async Task ListPaged_WithQueryStringDocumentParameterAndName_ShouldReturnNoRecord()
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
            var response = await Client.GetAsync("api/Customers?cpf=1234&Name=ghi");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Should().BeEmpty();
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
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Should().BeEmpty();
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
            var response = await Client.GetAsync("api/Customers?pageSize=3&page=1");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(3);
            customers.Total.Should().Be(3);
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
            var response = await Client.GetAsync("api/Customers?pageSize=1&page=3");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Total.Should().Be(3);
            customers.Data.First().Name.Should().Be("ghi");
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

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(expectedCount);
            customers.Total.Should().Be(expectedCount);
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
    }
}
