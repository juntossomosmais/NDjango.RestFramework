using AspNetCore.RestFramework.Core.Errors;
using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCore.RestFramework.Test.Core.BaseController
{
    public partial class BaseControllerTests
    {
        [Fact]
        public async Task Post_WithValidData_ShouldInsertObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

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
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "567", Name = "ac" };

            var content = new StringContent(JsonConvert.SerializeObject(customer), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PostAsync("api/Customers", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<ValidationErrors>(responseData);

            responseMessages.Error["Name"].Should().Contain("Name should have at least 3 characters");
            responseMessages.Error["CNPJ"].Should().Contain("CNPJ cannot be 567");

            var customers = dbSet.AsNoTracking().ToList();
            customers.Should().BeEmpty();
        }
    }
}
