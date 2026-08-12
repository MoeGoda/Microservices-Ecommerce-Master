using FluentValidation;

namespace Warehouse.Application.Features.Items.Commands.UpdateItemPrice
{
    public class UpdateItemPriceCommandValidator : AbstractValidator<UpdateItemPriceCommand>
    {
        public UpdateItemPriceCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.NewPrice).GreaterThanOrEqualTo(0);
        }
    }
}
