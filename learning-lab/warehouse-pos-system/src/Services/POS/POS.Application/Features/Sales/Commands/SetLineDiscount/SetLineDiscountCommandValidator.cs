using FluentValidation;

namespace POS.Application.Features.Sales.Commands.SetLineDiscount
{
    public class SetLineDiscountCommandValidator : AbstractValidator<SetLineDiscountCommand>
    {
        public SetLineDiscountCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.SaleLineId).GreaterThan(0);
            RuleFor(c => c.ManualDiscountPercent).InclusiveBetween(0, 100).When(c => c.ManualDiscountPercent.HasValue);
        }
    }
}
