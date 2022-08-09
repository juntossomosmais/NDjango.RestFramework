using AspNetCore.RestFramework.Core.Base;
using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Xunit;

namespace AspNetCore.RestFramework.Test.Core.BaseController
{
    public partial class BaseControllerTests
    {
        [Fact]
        public async Task ListPaged_ShouldReturn200OK()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?Name=ghi&CNPJ=789");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("ghi");
        }

        [Fact]
        public async Task ListPaged_WithIdQueryStringFilter_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi" });
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?id=35d948bd-ab3d-4446-912b-2d20c57c4935");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("abc");
        }

        [Fact]
        public async Task ListPaged_WithIdRangeQueryStringFilter_ShouldReturnTwoRecords()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { Id = Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"), CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { Id = Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"), CNPJ = "789", Name = "ghi" });
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?ids=6bdc2b9e-3710-40b9-93dd-c7558b446e21&ids=22ee1df9-c543-4509-a755-e7cd5dc0045e");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            Context.SaveChanges();

            var entities = dbSet.Where(m => new[] { "def", "ghi" }.Contains(m.Name)).AsNoTracking().ToList();

            // Act
            var response = await Client.GetAsync($"api/IntAsIdEntities?ids={entities[0].Id}&ids={entities[1].Id}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?Age=20");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?Age=20&SortDesc=Name,CNPJ");

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
        public async Task ListPaged_WithIntegerQueryStringAndSortAscParameter_ShouldReturnTwoRecordsSortedAsc()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { CNPJ = "124", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "456", Name = "def", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "123", Name = "abc", Age = 20 });
            dbSet.Add(new Customer() { CNPJ = "789", Name = "ghi", Age = 25 });
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?Age=20&Sort=Name,CNPJ");

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
        public async Task ListPaged_WithQueryStringDocumentParameter_ShouldReturnSingleRecord()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
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

            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=1234");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("abc");
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

            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=1234&Name=abc");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(1);
            customers.Data.FirstOrDefault().Name.Should().Be("abc");
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

            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=1234&Name=ghi");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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

            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?cpf=5557");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?pageSize=3&page=1");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync("api/Customers?pageSize=1&page=3");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
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
            dbSet.Add(new Customer() { Id = Guid.Parse("1a91f9ec-920b-4c92-83b0-6bf40d0209c2"), Age = 10, CNPJ = "76.637.568/0001-80", Name = "Agua Alta" });
            dbSet.Add(new Customer() { Id = Guid.Parse("a71bf8fa-0714-4281-8c51-23e763442919"), Age = 12, CNPJ = "24.451.215/0001-97", Name = "Agua Baixa" });
            dbSet.Add(new Customer() { Id = Guid.Parse("555b437e-3cd8-493c-b502-94cb9ba69a6b"), Age = 10, CNPJ = "81.517.224/0001-77", Name = "Bailão 12 Inc" });
            dbSet.Add(new Customer() { Id = Guid.Parse("f10ca31e-f60b-4d4e-8ca3-a754c4fda6bc"), Age = 25, CNPJ = "59.732.451/0001-66", Name = "Xablau Inc" });
            dbSet.Add(new Customer() { Id = Guid.Parse("c2710e39-f17a-469e-8994-28fd621819b4"), Age = 28, CNPJ = "55.387.453/0001-04", Name = "Problem Solver" });
            Context.SaveChanges();

            // Act
            var response = await Client.GetAsync($"api/Customers?search={HttpUtility.UrlEncode(term)}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var responseData = await response.Content.ReadAsStringAsync();
            var customers = JsonConvert.DeserializeObject<PagedBaseResponse<List<Customer>>>(responseData);
            customers.Data.Count.Should().Be(expectedCount);
            customers.Total.Should().Be(expectedCount);
        }
    }
}
