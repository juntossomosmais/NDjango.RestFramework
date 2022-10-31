using AspNetRestFramework.Sample.DTO;
using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public async Task PutMany_ShouldUpdateManyEntities_AndReturnTheirIds()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { Id = Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"), CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { Id = Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"), CNPJ = "789", Name = "ghi" });
            await Context.SaveChangesAsync();

            var customerData = new CustomerDto
            {
                CNPJ = "aaaa",
                Name = "eee"
            };

            var expectedGuids = new[] {
                Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"),
                Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"),
            };

            var content = new StringContent(JsonConvert.SerializeObject(customerData), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/Customers?ids=6bdc2b9e-3710-40b9-93dd-c7558b446e21&ids=22ee1df9-c543-4509-a755-e7cd5dc0045e", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var updatedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            updatedIds.Should().BeEquivalentTo(expectedGuids);

            var updatedEntities = dbSet.Where(m => expectedGuids.Contains(m.Id)).AsNoTracking();
            updatedEntities.Should().BeEquivalentTo(
                new[] {
                    new Customer() { CNPJ = "aaaa", Name = "eee" },
                    new Customer() { CNPJ = "aaaa", Name = "eee" },
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

            var content = new StringContent(JsonConvert.SerializeObject(entityToUpdate), Encoding.UTF8, "application/json-patch+json");

            // Act
            var response = await Client.PutAsync($"api/IntAsIdEntities?ids=1&ids=2", content);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.MethodNotAllowed);
        }
    }
}
