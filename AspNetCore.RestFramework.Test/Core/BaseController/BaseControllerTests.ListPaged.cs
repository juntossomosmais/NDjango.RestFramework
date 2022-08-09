using AspNetCore.RestFramework.Core.Base;
using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    }
}
