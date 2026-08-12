using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items
{
    // What a caller should actually charge for an item RIGHT NOW — the
    // one place that knows how to turn "an Item plus whatever Promotion
    // is active" into a single price. ResolveBarcodeQueryHandler (the
    // path a POS scan actually calls) and GetItemByIdQueryHandler both go
    // through this rather than each re-deriving the discount math, the
    // same "one implementation of what a stock change is" reasoning
    // StockAdjustmentStager applies to inventory.
    public class EffectivePriceResolver
    {
        private readonly IPromotionRepository _promotionRepository;

        public EffectivePriceResolver(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task<EffectivePrice> Resolve(Item item, DateTime nowUtc)
        {
            var promotion = await _promotionRepository.GetActiveForItem(item.Id, nowUtc);
            if (promotion is null)
            {
                return new EffectivePrice { UnitPrice = item.UnitPrice };
            }

            var discounted = promotion.DiscountType == DiscountType.PercentageOff
                ? item.UnitPrice * (1 - promotion.DiscountValue / 100m)
                : item.UnitPrice - promotion.DiscountValue;

            // A FixedAmountOff bigger than the item's own price is a
            // data-entry mistake somewhere upstream (CreatePromotionCommand
            // doesn't know the item's current price to validate against at
            // creation time) — floor at zero rather than letting a sale
            // line go negative.
            discounted = Math.Max(discounted, 0m);

            return new EffectivePrice
            {
                UnitPrice = decimal.Round(discounted, 2, MidpointRounding.AwayFromZero),
                OriginalUnitPrice = item.UnitPrice,
                PromotionId = promotion.Id,
            };
        }
    }

    public class EffectivePrice
    {
        public decimal UnitPrice { get; set; }
        public decimal? OriginalUnitPrice { get; set; }
        public int? PromotionId { get; set; }
    }
}
