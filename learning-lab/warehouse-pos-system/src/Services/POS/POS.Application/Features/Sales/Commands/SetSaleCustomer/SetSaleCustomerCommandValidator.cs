using FluentValidation;

namespace POS.Application.Features.Sales.Commands.SetSaleCustomer
{
    public class SetSaleCustomerCommandValidator : AbstractValidator<SetSaleCustomerCommand>
    {
        public SetSaleCustomerCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.CustomerId).GreaterThan(0).When(c => c.CustomerId.HasValue);
        }
    }
}
