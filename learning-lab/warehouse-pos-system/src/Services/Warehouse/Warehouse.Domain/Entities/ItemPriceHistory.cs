using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // One row per actual price change, not per price-check — recorded only
    // when UpdateItemPriceCommand's new price genuinely differs from the
    // Item's current one (see the handler). CreatedAt (EntityBase) is the
    // "when" this took effect; there's no separate EffectiveFrom because a
    // price change here takes effect immediately, unlike a Promotion's
    // scheduled StartsAtUtc/EndsAtUtc window.
    public class ItemPriceHistory : EntityBase
    {
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
    }
}
