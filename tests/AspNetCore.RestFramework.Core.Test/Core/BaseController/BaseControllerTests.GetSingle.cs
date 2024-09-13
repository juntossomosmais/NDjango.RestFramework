using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Errors;
using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.RestFramework.Test.Core.BaseController
{
    public partial class BaseControllerTests
    {
        [Fact]
        public async Task GetSingle_WithValidParameter_ShouldReturn1Record()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var customer2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            var customer3 = new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" };


            dbSet.AddRange(customer1, customer2, customer3);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{customer1.Id}");

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
            var dbSet = Context.Set<Customer>();
            var customer1 = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            var customer2 = new Customer() { Id = Guid.NewGuid(), CNPJ = "456", Name = "def" };
            var customer3 = new Customer() { Id = Guid.NewGuid(), CNPJ = "789", Name = "ghi" };

            dbSet.AddRange(customer1, customer2, customer3);
            await Context.SaveChangesAsync();

            // Act
            var response = await Client.GetAsync($"api/Customers/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
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
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var msg = JsonConvert.DeserializeObject<UnexpectedError>(responseData);
            msg.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
        }
    }
}
