using AspNetCore.RestFramework.Core.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace AspNetCore.RestFramework.Core.Extensions
{
    public static class ModelStateValidationExtensions
    {
        public static IMvcBuilder ConfigureValidationResponseFormat(this IMvcBuilder builder) =>
            builder.ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var response = new ValidationErrors(new Dictionary<string, string[]>());

                    foreach (var (key, value) in context.ModelState)
                        response.Error.Add(key, value.Errors.Select(e => e.ErrorMessage).ToArray());

                    return new BadRequestObjectResult(response);
                };
            });
    }
}
