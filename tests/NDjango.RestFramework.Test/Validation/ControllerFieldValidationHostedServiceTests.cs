using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NDjango.RestFramework.Test.Support;
using NDjango.RestFramework.Validation;
using Xunit;

namespace NDjango.RestFramework.Test.Validation;

public class ControllerFieldValidationHostedServiceTests
{
    private static ApplicationPartManager CreatePartManagerFor(params Type[] controllerTypes)
    {
        var partManager = new ApplicationPartManager();
        partManager.FeatureProviders.Add(new SpecificControllerFeatureProvider(controllerTypes));
        return partManager;
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var factory = new WebApplicationFactory<FakeProgram>();
        _ = factory.CreateClient();
        return factory.Services;
    }

    [Fact]
    public async Task StartAsync_WhenAllControllersAreValid_ShouldNotThrow()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var partManager = CreatePartManagerFor(
            typeof(CustomersController),
            typeof(CustomerDocumentsController),
            typeof(IntAsIdEntitiesController));
        var service = new ControllerFieldValidationHostedService(serviceProvider, partManager);

        // Act
        var exception = await Record.ExceptionAsync(() =>
            service.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_WhenControllerHasInvalidGetFields_ShouldThrowWithDescriptiveMessage()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var partManager = CreatePartManagerFor(typeof(InvalidFieldEntitiesController));
        var service = new ControllerFieldValidationHostedService(serviceProvider, partManager);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));

        // Assert
        Assert.Contains("InvalidFieldEntitiesController", exception.Message);
        Assert.Contains("NonExistentField", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenControllerHasInvalidAllowedFields_ShouldThrowWithDescriptiveMessage()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var partManager = CreatePartManagerFor(typeof(InvalidAllowedFieldEntitiesController));
        var service = new ControllerFieldValidationHostedService(serviceProvider, partManager);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));

        // Assert
        Assert.Contains("InvalidAllowedFieldEntitiesController", exception.Message);
        Assert.Contains("NonExistentAllowedField", exception.Message);
    }

    [Fact]
    public async Task StartAsync_WhenControllerGetFieldsThrows_ShouldThrowWithDescriptiveMessage()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var partManager = CreatePartManagerFor(typeof(SellersController));
        var service = new ControllerFieldValidationHostedService(serviceProvider, partManager);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(CancellationToken.None));

        // Assert
        Assert.Contains("SellersController", exception.Message);
    }

    private class SpecificControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly Type[] _controllerTypes;

        public SpecificControllerFeatureProvider(params Type[] controllerTypes)
        {
            _controllerTypes = controllerTypes;
        }

        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var type in _controllerTypes)
                feature.Controllers.Add(type.GetTypeInfo());
        }
    }
}
