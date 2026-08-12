using FluentValidation;

namespace POS.Application.Features.Sales.Commands.AddSaleLine
{
    public class AddSaleLineCommandValidator : AbstractValidator<AddSaleLineCommand>
    {
        public AddSaleLineCommandValidator()
        {
            RuleFor(c => c.SaleId).GreaterThan(0);
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.Sku).NotEmpty().MaximumLength(50);
            RuleFor(c => c.ItemName).NotEmpty().MaximumLength(200);
            RuleFor(c => c.UnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(c => c.Quantity).GreaterThan(0);
        }
    }
}
