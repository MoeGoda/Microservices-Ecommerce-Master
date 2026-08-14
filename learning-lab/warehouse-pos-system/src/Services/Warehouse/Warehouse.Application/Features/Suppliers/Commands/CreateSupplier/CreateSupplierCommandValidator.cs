using FluentValidation;

namespace Warehouse.Application.Features.Suppliers.Commands.CreateSupplier
{
    public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
    {
        public CreateSupplierCommandValidator()
        {
            RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
            RuleFor(c => c.ContactName).MaximumLength(200);
            RuleFor(c => c.Email).EmailAddress().MaximumLength(200).When(c => !string.IsNullOrEmpty(c.Email));
            RuleFor(c => c.Phone).MaximumLength(50);
            RuleFor(c => c.Address).MaximumLength(500);
        }
    }
}
