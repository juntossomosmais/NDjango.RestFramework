using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NDjango.RestFramework.Errors;

namespace NDjango.RestFramework.Extensions
{
    public static class ModelStateValidationExtensions
    {
        public static IMvcBuilder ConfigureValidationResponseFormat(this IMvcBuilder builder) =>
            builder.ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = new Dictionary<string, string[]>();

                    foreach (var (key, value) in context.ModelState)
                        errors.Add(key, value.Errors.Select(e => e.ErrorMessage).ToArray());

                    return new BadRequestObjectResult(new ValidationErrors(errors));
                };
            });
    }
}
