using FluentValidation;

namespace Warehouse.Application.Features.Stock.Commands.AdjustStock
{
    public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
    {
        public AdjustStockCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.LocationId).GreaterThan(0);

            // A zero adjustment is a no-op that would still write a
            // meaningless StockTransaction row — reject it rather than
            // silently accepting it.
            RuleFor(c => c.QuantityChange).NotEqual(0);

            RuleFor(c => c.Reference).MaximumLength(100);
        }
    }
}
