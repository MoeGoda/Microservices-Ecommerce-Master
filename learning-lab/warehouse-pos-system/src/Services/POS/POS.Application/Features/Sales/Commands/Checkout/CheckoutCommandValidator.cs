using FluentValidation;

namespace POS.Application.Features.Sales.Commands.Checkout
{
    public class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
    {
        public CheckoutCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
        }
    }
}
