using AspNetCore.RestFramework.Core.Base;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AspNetRestFramework.Sample.Models;
using Xunit;
using AspNetCore.RestFramework.Core.Errors;

namespace AspNetCore.RestFramework.Test.Core.BaseController
{
    public partial class BaseControllerTests : IntegrationTestBase
    {
        [Fact]
        public async Task SwaggerJson_ShouldReturnStatus200()
        {
            // Arrange
            var urlSwagger = "/swagger/v1/swagger.json";

            // Act
            var response = await Client.GetAsync(urlSwagger);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }
}
