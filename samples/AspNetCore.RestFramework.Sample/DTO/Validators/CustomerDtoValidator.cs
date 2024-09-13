using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace AspNetRestFramework.Sample.DTO.Validators
{
    public class CustomerDtoValidator : AbstractValidator<CustomerDto>
    {
        public CustomerDtoValidator(IHttpContextAccessor context)
        {
            RuleFor(m => m.Name)
                .MinimumLength(3)
                .WithMessage("Name should have at least 3 characters");

            if (context.HttpContext.Request.Method == HttpMethods.Post)
                RuleFor(m => m.CNPJ)
                    .NotEqual("567")
                    .WithMessage("CNPJ cannot be 567");
        }
    }
}
