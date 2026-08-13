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

            // F2 — FixedAmountOff had no upper bound at all: a flat
            // discount bigger than any real item's price is already
            // harmless in practice (EffectivePriceResolver floors the
            // discounted price at zero rather than letting a sale line go
            // negative), but this validator can't see the item's actual
            // price to reject it more precisely — it has no DB access,
            // and shouldn't. This cap rejects an obvious data-entry
            // mistake (or overflow-style input) at the door instead of
            // relying solely on the apply-time floor to make it harmless.
            RuleFor(c => c.DiscountValue)
                .LessThanOrEqualTo(1_000_000)
                .When(c => c.DiscountType == DiscountType.FixedAmountOff)
                .WithMessage("A fixed discount amount is unreasonably large.");

            RuleFor(c => c.EndsAtUtc).GreaterThan(c => c.StartsAtUtc);
        }
    }
}
