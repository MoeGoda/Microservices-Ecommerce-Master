using FluentValidation;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.CreatePromotion
{
    public class CreatePromotionCommandValidator : AbstractValidator<CreatePromotionCommand>
    {
        public CreatePromotionCommandValidator()
        {
            RuleFor(c => c.ItemId).GreaterThan(0);
            RuleFor(c => c.DiscountType).IsInEnum();
            RuleFor(c => c.DiscountValue).GreaterThan(0);

            // A PercentageOff above 100 would mean paying the customer to
            // take it — only meaningful for PercentageOff, so it's a
            // conditional rule rather than a blanket max on DiscountValue.
            RuleFor(c => c.DiscountValue)
                .LessThanOrEqualTo(100)
                .When(c => c.DiscountType == DiscountType.PercentageOff)
                .WithMessage("A percentage discount can't exceed 100%.");

            RuleFor(c => c.EndsAtUtc).GreaterThan(c => c.StartsAtUtc);
        }
    }
}
