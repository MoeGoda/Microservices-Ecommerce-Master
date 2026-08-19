using FluentValidation;

namespace POS.Application.Features.Customers.Commands.UpdateCustomer
{
    public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
    {
        public UpdateCustomerCommandValidator()
        {
            RuleFor(c => c.Id).GreaterThan(0);
            RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Phone).MaximumLength(30);
            RuleFor(c => c.Email).EmailAddress().MaximumLength(200).When(c => !string.IsNullOrEmpty(c.Email));
        }
    }
}
