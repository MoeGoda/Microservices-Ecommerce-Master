using FluentValidation;

namespace POS.Application.Features.Sales.Commands.AddSaleLine
{
    public class AddSaleLineCommandValidator : AbstractValidator<AddSaleLineCommand>
    {
        public AddSaleLineCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.Barcode).NotEmpty().MaximumLength(50);
            RuleFor(c => c.Quantity).GreaterThan(0);
            RuleFor(c => c.ManualDiscountPercent).InclusiveBetween(0, 100).When(c => c.ManualDiscountPercent.HasValue);
        }
    }
}
