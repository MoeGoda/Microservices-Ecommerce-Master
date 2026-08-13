using FluentValidation;

namespace POS.Application.Features.Sales.Commands.ReturnSale
{
    public class ReturnSaleCommandValidator : AbstractValidator<ReturnSaleCommand>
    {
        public ReturnSaleCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
        }
    }
}
