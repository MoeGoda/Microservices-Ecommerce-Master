using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // Scoped to a single Item on purpose, not a Category or "all items" —
    // the smallest slice that's still genuinely useful (a real markdown on
    // a real product), leaving a category-wide/storewide promotion as the
    // natural next step the moment a SECOND scope is actually needed,
    // rather than guessing at that shape now (same "extract on second use"
    // discipline as StockAdjustmentStager/JwtTokenFactory elsewhere in
    // this codebase, just applied to NOT building something yet).
    public class Promotion : EntityBase
    {
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public DiscountType DiscountType { get; set; }

        // A percentage (0-100) when DiscountType is PercentageOff, or a
        // currency amount when FixedAmountOff — which one it means is
        // entirely determined by DiscountType, the same "one column, two
        // meanings depending on a sibling enum" shape StockTransaction's
        // Reference has relative to Reason.
        public decimal DiscountValue { get; set; }

        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
    }
}
