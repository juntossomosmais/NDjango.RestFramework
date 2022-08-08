using AspNetCore.RestFramework.Core.Base;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AspNetRestFramework.Sample;
using AspNetRestFramework.Sample.Context;
using AspNetRestFramework.Sample.Models;
using Xunit;
using AspNetCore.RestFramework.Core.Errors;

namespace AspNetCore.RestFramework.Test.Core
{
    [Collection("Database Collection")]
    public class ControllerIntegrationTests
    {
        private readonly TestServer _server;
        private readonly ApplicationDbContext _context;
        
        public ControllerIntegrationTests()
        {
            var applicationPath = Directory.GetCurrentDirectory();

            _server = new TestServer(new WebHostBuilder()
                .UseEnvironment("Testing")
                .UseContentRoot(applicationPath)
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(applicationPath)
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build()
                )
                .UseStartup<Startup>());

            _context = (ApplicationDbContext)_server.Services.GetService(typeof(ApplicationDbContext));

            ClearDatabase();
        }

        private void ClearDatabase()
        {
            _context.Database.ExecuteSqlRaw(@"
                EXEC sp_msforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';
                EXEC sp_msforeachtable 'DELETE FROM ?';
                EXEC sp_msforeachtable 'ALTER TABLE ? CHECK CONSTRAINT all';
            ");
        }

        [Fact]
        public async Task TestGet()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);

            customers.Data.Count.Should().Be(3);
        }

        [Fact]
        public async Task Get_WithQueryString_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?Name=ghi&CNPJ=789");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("ghi");
        }

        [Fact]
        public async Task Get_WithIntegerQueryString_ShouldReturnTwoRecords()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?Age=20");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(2);
        }

        [Fact]
        public async Task Get_WithIntegerQueryStringAndSortDescParameter_ShouldReturnTwoRecordsSortedDesc()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "124", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?Age=20&SortDesc=Name,CNPJ");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(3);

            var first = customers.Data.FirstOrDefault();
            var second = customers.Data.Skip(1).FirstOrDefault();
            var third = customers.Data.Skip(2).FirstOrDefault();

            first.Name.Should().Be("def");
            first.CNPJ.Should().Be("456");
            second.Name.Should().Be("abc");
            second.CNPJ.Should().Be("124");
            third.Name.Should().Be("abc");
            third.CNPJ.Should().Be("123");
        }

        [Fact]
        public async Task Get_WithIntegerQueryStringAndSortAscParameter_ShouldReturnTwoRecordsSortedAsc()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "124", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?Age=20&Sort=Name,CNPJ");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(3);

            var first = customers.Data.FirstOrDefault();
            var second = customers.Data.Skip(1).FirstOrDefault();
            var third = customers.Data.Skip(2).FirstOrDefault();

            first.Name.Should().Be("abc");
            first.CNPJ.Should().Be("123");
            second.Name.Should().Be("abc");
            second.CNPJ.Should().Be("124");
            third.Name.Should().Be("def");
            third.CNPJ.Should().Be("456");
        }

        [Fact]
        public async Task Get_WithQueryStringDocumentParameter_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "XYZ"
                }, new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cpf",
                    Document = "1234"
                }}

            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "LHA"
                }}
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?cpf=1234");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("abc");
        }

        [Fact]
        public async Task Get_WithQueryStringDocumentParameterAndName_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "XYZ"
                }, new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cpf",
                    Document = "1234"
                }}

            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "LHA"
                }}
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?cpf=1234&Name=abc");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("abc");
        }

        [Fact]
        public async Task Get_WithQueryStringDocumentParameterAndName_ShouldReturnNoRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "XYZ"
                }, new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cpf",
                    Document = "1234"
                }}

            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "LHA"
                }}
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?cpf=1234&Name=ghi");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Should().BeEmpty();
        }
        [Fact]
        public async Task Get_WithQueryStringCustomerParameter_ShouldReturnNoRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer()
            {
                CNPJ = "123",
                Name = "abc",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "XYZ"
                }, new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cpf",
                    Document = "1234"
                }}

            });
            dbSet.Add(new Customer()
            {
                CNPJ = "456",
                Name = "def",
                CustomerDocument = new List<CustomerDocument>() { new CustomerDocument
                {
                    Id = Guid.NewGuid(),
                    DocumentType = "cnpj",
                    Document = "LHA"
                }}
            });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });

            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?cpf=5557");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Should().BeEmpty();
        }
        [Fact]
        public async Task Patch_WithFullObject_ShouldUpdateFullObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8, "application/json-patch+json");
            
            // Act
            var response = await _client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            updatedCustomer.Name.Should().Be(customerToUpdate.Name);
            updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);
        }

        [Fact]
        public async Task Patch_WithPartialObject_ShouldUpdatePartialObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8, "application/json-patch+json");
            
            // Act
            var response = await _client.PatchAsync($"api/Customers/{customer.Id}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            updatedCustomer.Name.Should().Be(customer.Name);
            updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);
        }

        [Fact]
        public async Task Post_WithValidData_ShouldInsertObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json-patch+json");
            
            // Act
            var response = await _client.PostAsync("api/Customers", content);

            // Assert
            var addedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
            addedCustomer.Name.Should().Be(customer.Name);
            addedCustomer.CNPJ.Should().Be(customer.CNPJ);
        }

        [Fact]
        public async Task Post_WithInvalidData_ShouldNotInsertObjectAndReturn400Error()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "567", Name = "ac" };

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json-patch+json");
            
            // Act
            var response = await _client.PostAsync("api/Customers", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<ValidationErrors>(responseData);

            responseMessages.Error["Name"].Should().Contain("Name should have at least 3 characters");
            responseMessages.Error["CNPJ"].Should().Contain("CNPJ cannot be 567");

            var customers = dbSet.AsNoTracking().ToList();
            customers.Should().BeEmpty();
        }

        [Fact]
        public async Task Get_WithPageSize1AndPageSize3_ShouldReturn1RecordAnd3Pages()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            // Act
            var response = await _client.GetAsync("api/Customers?pageSize=3&page=1");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(3);
            customers.Total.Should().Be(3);
        }

        [Fact]
        public async Task Get_WithPageSize1AndPageSize3_ShouldReturn1RecordAnd1Pages()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync("api/Customers?pageSize=1&page=3");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Total.Should().Be(3);
            customers.Data.First().Name.Should().Be("ghi");
        }

        [Fact]
        public async Task GetSingle_WithValidParameter_ShouldReturn1Record()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var customer2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            var customer3 = new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" };


            dbSet.AddRange(customer1, customer2, customer3);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync($"api/Customers/{customer1.Id}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customer = JsonConvert.DeserializeObject<Customer>(responseData);
            customer.Should().NotBeNull();
            customer.Id.Should().Be(customer1.Id);
        }

        [Fact]
        public async Task GetSingle_WithInValidParameter_ShouldReturn404()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var customer2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            var customer3 = new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" };

            dbSet.AddRange(customer1, customer2, customer3);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync($"api/Customers/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Patch_WithPartialObject_ShouldReturnMethodNowAllowed()
        {
            // Arrange
            var dbSet = _context.Set<Seller>();
            var seller = new Seller() { Id = Guid.NewGuid(), Name = "abc" };
            dbSet.Add(seller);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            var customerToUpdate = new
            {
                Id = seller.Id,
                CNPJ = "aaaa",
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8, "application/json-patch+json");
            
            // Act
            var response = await _client.PatchAsync($"api/Sellers/{seller.Id}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.MethodNotAllowed);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == seller.Id);
        }

        [Fact]
        public async Task Put_WithFullObject_ShouldUpdateFullObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8, "application/json-patch+json");
            
            // Act
            var response = await _client.PutAsync($"api/Customers/{customer.Id}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            updatedCustomer.Name.Should().Be(customerToUpdate.Name);
            updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);
        }

        [Fact]
        public async Task Delete_WithObject_ShouldDeleteEntityFromDataseAndReturnOk()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");

            // Act
            var response = await _client.DeleteAsync($"api/Customers/{customer.Id}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            updatedCustomer.Should().BeNull();
        }

        [Fact]
        public async Task GetCustomer_ShouldReturnStatus200()
        {
            // Arrange
            var urlSwagger = "/swagger/v1/swagger.json";
            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");

            // Act
            var response = await _client.GetAsync(urlSwagger);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetSingle_WithoutFields_ShouldReturnBadRequest()
        {
            // Arrange
            var dbSet = _context.Set<Seller>();
            var seller1 = new Seller() { Id = Guid.NewGuid(), Name = "Teste" };

            dbSet.Add(seller1);
            _context.SaveChanges();

            var _client = _server.CreateClient();
            _client.BaseAddress = new System.Uri("http://localhost:35185");
            
            // Act
            var response = await _client.GetAsync($"api/Sellers/{seller1.Id}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var msg = JsonConvert.DeserializeObject<UnexpectedError>(responseData);
            msg.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
        }
    }
}
