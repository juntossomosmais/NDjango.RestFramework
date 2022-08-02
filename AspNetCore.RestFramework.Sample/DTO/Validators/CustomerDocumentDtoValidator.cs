using FluentValidation;

namespace AspNetRestFramework.Sample.DTO.Validators
{
    public class CustomerDocumentDtoValidator : AbstractValidator<CustomerDocumentDto>
    {
        public CustomerDocumentDtoValidator()
        {
            RuleFor(m => m.Document)
                .MinimumLength(3)
                .WithMessage("Name should have at least 3 characters");
        }
    }
}
