using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NDjango.RestFramework.Base;

namespace NDjango.RestFramework.Validation;

internal sealed class ControllerFieldValidationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApplicationPartManager _partManager;

    public ControllerFieldValidationHostedService(
        IServiceProvider serviceProvider,
        ApplicationPartManager partManager)
    {
        _serviceProvider = serviceProvider;
        _partManager = partManager;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var feature = new ControllerFeature();
        _partManager.PopulateFeature(feature);

        var errors = new List<string>();

        foreach (var controllerTypeInfo in feature.Controllers)
        {
            var controllerType = controllerTypeInfo.AsType();

            if (!typeof(IFieldConfigurableController).IsAssignableFrom(controllerType))
                continue;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var controller = (IFieldConfigurableController)ActivatorUtilities
                    .CreateInstance(scope.ServiceProvider, controllerType);

                // GetFields() was validated in the base constructor (ResolveAndValidateFields).
                // If it threw, we caught it above.

                // Validate AllowedFields
                var allowedFields = controller.GetAllowedFieldsConfiguration();
                if (allowedFields.Length > 0)
                {
                    var destinationType = controller.GetDestinationType();
                    var propertyNames = destinationType
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Select(p => p.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    var invalidFields = allowedFields
                        .Where(f => !propertyNames.Contains(f))
                        .ToList();

                    if (invalidFields.Count > 0)
                    {
                        errors.Add(
                            $"{controllerType.Name}: AllowedFields contains invalid fields " +
                            $"[{string.Join(", ", invalidFields)}] for {destinationType.Name}. " +
                            $"Valid properties: [{string.Join(", ", propertyNames)}].");
                    }
                }

                // Validate per-field validation hook names
                var misnamedHooks = controller.GetMisnamedValidationHooks();
                if (misnamedHooks.Count > 0)
                {
                    errors.Add(
                        $"{controllerType.Name}: Serializer contains validation hooks that do not match " +
                        $"any property on the DTO: [{string.Join(", ", misnamedHooks)}]. " +
                        $"Ensure the property name between 'Validate' and 'Async' matches a DTO property.");
                }

                // Dispose the controller if it implements IDisposable
                if (controller is IDisposable disposable)
                    disposable.Dispose();
            }
            catch (Exception ex)
            {
                errors.Add($"{controllerType.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Controller field validation failed:\n" + string.Join("\n", errors));
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
