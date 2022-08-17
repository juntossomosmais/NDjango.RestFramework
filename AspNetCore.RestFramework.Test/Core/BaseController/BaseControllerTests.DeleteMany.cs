using AspNetRestFramework.Sample.Models;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        public async Task DeleteMany_ShouldDeleteManyEntities_AndReturnTheirIds()
        {
            // Arrange
            var dbSet = Context.Set<Customer>();
            dbSet.Add(new Customer() { Id = Guid.Parse("35d948bd-ab3d-4446-912b-2d20c57c4935"), CNPJ = "123", Name = "abc" });
            dbSet.Add(new Customer() { Id = Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"), CNPJ = "456", Name = "def" });
            dbSet.Add(new Customer() { Id = Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"), CNPJ = "789", Name = "ghi" });
            Context.SaveChanges();

            var expectedGuids = new[] {
                Guid.Parse("6bdc2b9e-3710-40b9-93dd-c7558b446e21"),
                Guid.Parse("22ee1df9-c543-4509-a755-e7cd5dc0045e"),
            };

            // Act
            var response = await Client.DeleteAsync($"api/Customers?ids=6bdc2b9e-3710-40b9-93dd-c7558b446e21&ids=22ee1df9-c543-4509-a755-e7cd5dc0045e");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            var deletedIds = JsonConvert.DeserializeObject<List<Guid>>(await response.Content.ReadAsStringAsync());
            deletedIds.Should().BeEquivalentTo(expectedGuids);

            var entitiesWithDeletedIds = dbSet.Where(m => expectedGuids.Contains(m.Id)).AsNoTracking();
            entitiesWithDeletedIds.Should().BeEmpty();
        }
    }
}
