using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Errors;
using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.RestFramework.Test.Core.BaseController
{
    public partial class BaseControllerTests
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
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
            updatedCustomer.Should().BeNull();
        }

        [Fact]
        public async Task Delete_WhenEntityDoesntExist_ReturnsNotFound()
        {
            // Act
            var response = await Client.DeleteAsync($"api/Customers/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Delete_WhenEntityDoesntImplementGetFields_ReturnsBadRequest()
        {
            // Act
            var response = await Client.DeleteAsync($"api/Sellers/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
        }
    }
}
