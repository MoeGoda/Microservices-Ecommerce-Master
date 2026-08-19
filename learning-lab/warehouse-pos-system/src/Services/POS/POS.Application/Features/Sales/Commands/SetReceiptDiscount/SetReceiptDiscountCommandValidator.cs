using FluentValidation;

namespace POS.Application.Features.Sales.Commands.SetReceiptDiscount
{
    public class SetReceiptDiscountCommandValidator : AbstractValidator<SetReceiptDiscountCommand>
    {
        public SetReceiptDiscountCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.ManualReceiptDiscountPercent).InclusiveBetween(0, 100).When(c => c.ManualReceiptDiscountPercent.HasValue);
        }
    }
}
