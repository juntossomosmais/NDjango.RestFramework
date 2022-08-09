﻿using AspNetCore.RestFramework.Core.Base;
using AspNetCore.RestFramework.Core.Errors;
using AspNetRestFramework.Sample.DTO;
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
        public async Task Put_WithFullObject_ShouldUpdateFullObject()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            var customer = new Customer() { Id = Guid.NewGuid(), CNPJ = "123", Name = "abc" };
            dbSet.Add(customer);
            Context.SaveChanges();

            var customerToUpdate = new
            {
                Id = customer.Id,
                CNPJ = "aaaa",
                Name = "eee"
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{customer.Id}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedCustomer = dbSet.AsNoTracking().FirstOrDefault(x => x.Id == customer.Id);
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

            var content = new StringContent(JsonConvert.SerializeObject(customerToUpdate), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Customers/{Guid.NewGuid()}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
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

            var content = new StringContent(JsonConvert.SerializeObject(seller), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Sellers/{seller.Id}", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
            var responseData = await response.Content.ReadAsStringAsync();
            var responseMessages = JsonConvert.DeserializeObject<UnexpectedError>(responseData);

            responseMessages.Error["msg"].Should().Be(BaseMessages.ERROR_GET_FIELDS);
        }
    }
}
