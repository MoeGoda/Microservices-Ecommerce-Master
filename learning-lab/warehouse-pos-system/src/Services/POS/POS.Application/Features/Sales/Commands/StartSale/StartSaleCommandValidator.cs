using FluentValidation;

namespace POS.Application.Features.Sales.Commands.StartSale
{
    public class StartSaleCommandValidator : AbstractValidator<StartSaleCommand>
    {
        public StartSaleCommandValidator()
        {
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.CashierUserId).GreaterThan(0);
        }
    }
}
