using FluentValidation;

namespace Warehouse.Application.Features.Items.Commands.AddItemUnit
{
    public class AddItemUnitCommandValidator : AbstractValidator<AddItemUnitCommand>
    {
        public AddItemUnitCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.UnitOfMeasureId).GreaterThan(0);
            RuleFor(c => c.ConversionFactor).GreaterThan(0);
        }
    }
}
