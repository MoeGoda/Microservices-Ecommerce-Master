using FluentValidation;

namespace POS.Application.Features.Sales.Commands.CancelSale
{
    public class CancelSaleCommandValidator : AbstractValidator<CancelSaleCommand>
    {
        public CancelSaleCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
        }
    }
}
