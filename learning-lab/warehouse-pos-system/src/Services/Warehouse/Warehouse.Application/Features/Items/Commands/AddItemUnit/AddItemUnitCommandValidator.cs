using FluentValidation;

namespace Warehouse.Application.Features.Items.Commands.AddItemUnit
{
    public class AddItemUnitCommandValidator : AbstractValidator<AddItemUnitCommand>
    {
        public AddItemUnitCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.UnitOfMeasureId).GreaterThan(0);

            // F2 — an upper bound alongside the existing GreaterThan(0):
            // no real "1 BOX = N base units" conversion is ever anywhere
            // near this large; this exists to reject an obvious data-entry
            // mistake or overflow-style input, not to encode a business
            // rule about what conversion factors are realistic.
            RuleFor(c => c.ConversionFactor).GreaterThan(0).LessThanOrEqualTo(100_000);
        }
    }
}
