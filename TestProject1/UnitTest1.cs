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
using WebApplication2;
using WebApplication2.Base;
using WebApplication2.Context;
using WebApplication2.Models;
using Xunit;

namespace TestProject1
{
    public class UnitTest1
    {
        private TestServer _server;
        private ApplicationDbContext _context;
        public UnitTest1()
        {

            var applicationPath = Directory.GetCurrentDirectory();
            _server = new TestServer(new WebHostBuilder()
                .UseEnvironment("Testing")
                .UseContentRoot(applicationPath)
                .UseConfiguration(new ConfigurationBuilder()
                    .SetBasePath(applicationPath)
                    .AddJsonFile("appsettings.json")
                    .Build()
                )
                .UseStartup<Startup>());

            _context = (ApplicationDbContext)_server.Services.GetService(typeof(ApplicationDbContext));
        }


        [Fact]
        public async Task TestGet()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(3);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }


        [Fact]
        public async Task Get_WithQueryString_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(1);
                customers.Data.FirstOrDefault().Name.Should().Be("ghi");

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Get_WithIntegerQueryString_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(2);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Get_WithQueryStringDocumentParameter_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
                dbSet.Add(new Customer()
                {
                    CNPJ = "123",
                    Name = "abc",
                    CustomerDocuments = new List<CustomerDocument>() { new CustomerDocument
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
                    CustomerDocuments = new List<CustomerDocument>() { new CustomerDocument
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(1);
                customers.Data.FirstOrDefault().Name.Should().Be("abc");

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Get_WithQueryStringDocumentParameterAndName_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
                dbSet.Add(new Customer()
                {
                    CNPJ = "123",
                    Name = "abc",
                    CustomerDocuments = new List<CustomerDocument>() { new CustomerDocument
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
                    CustomerDocuments = new List<CustomerDocument>() { new CustomerDocument
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(1);
                customers.Data.FirstOrDefault().Name.Should().Be("abc");

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        public async Task Get_WithQueryStringDocumentParameterAndName_ShouldReturnNoRecord()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
                dbSet.Add(new Customer()
                {
                    CNPJ = "123",
                    Name = "abc",
                    CustomerDocuments = new List<CustomerDocument>() { new CustomerDocument
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
                    CustomerDocuments = new List<CustomerDocument>() { new CustomerDocument
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
                List<Customer> customers = JsonConvert.DeserializeObject<List<Customer>>(responseData);
                customers.Should().BeEmpty();

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }


        [Fact]
        public async Task Patch_WithFullObject_ShouldUpdateFullObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var response = await _client.PatchAsync("api/Customers", content);

                // Assert
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
                var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
                updatedCustomer.Name.Should().Be(customerToUpdate.Name);
                updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Patch_WithPartialObject_ShouldUpdatePartialObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var response = await _client.PatchAsync("api/Customers", content);

                // Assert
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
                var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
                updatedCustomer.Name.Should().Be(customer.Name);
                updatedCustomer.CNPJ.Should().Be(customerToUpdate.CNPJ);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Add_WithValidData_ShouldInsertObject()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();

            try
            {
                var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
                dbSet.Add(customer);

                var _client = _server.CreateClient();
                _client.BaseAddress = new System.Uri("http://localhost:35185");

                var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json-patch+json");
                // Act
                var response = await _client.PostAsync("api/Customers", content);

                // Assert
                var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.Created);
                updatedCustomer.Name.Should().Be(customer.Name);
                updatedCustomer.CNPJ.Should().Be(customer.CNPJ);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
            }
            catch (Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }


        }

        [Fact]
        public async Task Get_WithPageSize1AndPageSize3_ShouldReturn1RecordAnd3Pages()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(3);
                customers.Pages.Should().Be(1);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Get_WithPageSize1AndPageSize3_ShouldReturn1RecordAnd1Pages()
        {
            // Arrange
            var dbSet = _context.Set<Customer>();
            try
            {
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
                var customers = JsonConvert.DeserializeObject<PagedBaseResponse<Customer>>(responseData);
                customers.Data.Count.Should().Be(1);
                customers.Pages.Should().Be(3);
                customers.Data.First().Name.Should().Be("ghi");


                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }

        [Fact]
        public async Task Patch_WithPartialObject_ShouldReturnMethodNowAllowed()
        {
            // Arrange
            var dbSet = _context.Set<Seller>();
            try
            {
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
                var response = await _client.PatchAsync("api/Seller", content);

                // Assert
                response.StatusCode.Should().Be(System.Net.HttpStatusCode.MethodNotAllowed);
                var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == seller.Id);

                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();

            }
            catch (System.Exception)
            {
                dbSet.RemoveRange(dbSet.ToList());
                _context.SaveChanges();
                throw;
            }
        }
    }
}
