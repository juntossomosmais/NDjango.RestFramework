using Microsoft.Extensions.DependencyInjection;
using NDjango.RestFramework.Validation;

namespace NDjango.RestFramework.Extensions;

public static class ControllerFieldValidationExtensions
{
    /// <summary>
    /// Validates that all BaseController field configurations (GetFields and AllowedFields)
    /// reference valid properties on their entity types. The application will fail to start
    /// if any controller is misconfigured.
    /// </summary>
    public static IServiceCollection ValidateControllerFieldsOnStartup(
        this IServiceCollection services)
    {
        services.AddHostedService<ControllerFieldValidationHostedService>();
        return services;
    }
}
