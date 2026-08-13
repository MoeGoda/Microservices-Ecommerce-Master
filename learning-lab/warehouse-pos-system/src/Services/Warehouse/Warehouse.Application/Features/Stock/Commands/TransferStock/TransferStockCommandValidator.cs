using FluentValidation;

namespace Warehouse.Application.Features.Stock.Commands.TransferStock
{
    public class TransferStockCommandValidator : AbstractValidator<TransferStockCommand>
    {
        public TransferStockCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.FromLocationId).GreaterThan(0);
            RuleFor(c => c.ToLocationId).GreaterThan(0);
            RuleFor(c => c.Quantity).GreaterThan(0);
            RuleFor(c => c.Reference).MaximumLength(100);

            // A same-location "transfer" isn't a business operation this
            // command means to support — it would stage a -Q and a +Q at
            // the SAME (item, location) pair, netting to zero stock change
            // but still writing two meaningless StockTransaction rows.
            RuleFor(c => c).Must(c => c.FromLocationId != c.ToLocationId)
                .WithMessage("FromLocationId and ToLocationId must be different.");
        }
    }
}
