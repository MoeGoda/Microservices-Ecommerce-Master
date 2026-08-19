using FluentValidation;

namespace POS.Application.Features.Customers.Commands.CreateCustomer
{
    public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
    {
        public CreateCustomerCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Phone).MaximumLength(30);
            RuleFor(c => c.Email).EmailAddress().MaximumLength(200).When(c => !string.IsNullOrEmpty(c.Email));
        }
    }
}
