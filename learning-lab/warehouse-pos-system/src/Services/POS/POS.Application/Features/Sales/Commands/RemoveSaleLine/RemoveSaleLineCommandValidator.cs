using FluentValidation;

namespace POS.Application.Features.Sales.Commands.RemoveSaleLine
{
    public class RemoveSaleLineCommandValidator : AbstractValidator<RemoveSaleLineCommand>
    {
        public RemoveSaleLineCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.SaleLineId).GreaterThan(0);
        }
    }
}
