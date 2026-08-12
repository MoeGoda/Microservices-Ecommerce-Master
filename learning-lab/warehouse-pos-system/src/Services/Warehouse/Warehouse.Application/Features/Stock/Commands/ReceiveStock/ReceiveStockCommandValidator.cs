using FluentValidation;

namespace Warehouse.Application.Features.Stock.Commands.ReceiveStock
{
    public class ReceiveStockCommandValidator : AbstractValidator<ReceiveStockCommand>
    {
        public ReceiveStockCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.Quantity).GreaterThan(0);
            RuleFor(c => c.UnitOfMeasureId).GreaterThan(0);
            RuleFor(c => c.Reference).MaximumLength(100);
        }
    }
}
