using FluentValidation;

namespace Warehouse.Application.Features.Items.Commands.CancelPromotion
{
    public class CancelPromotionCommandValidator : AbstractValidator<CancelPromotionCommand>
    {
        public CancelPromotionCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.PromotionId).GreaterThan(0);
        }
    }
}
