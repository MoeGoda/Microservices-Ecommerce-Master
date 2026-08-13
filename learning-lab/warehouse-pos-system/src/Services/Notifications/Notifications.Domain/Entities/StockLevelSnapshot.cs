using Notifications.Domain.Common;

namespace Notifications.Domain.Entities
{
    // Exactly one row per (ItemId, LocationId) — Notifications' own tiny,
    // purpose-built copy of "what was the quantity/threshold last time we
    // heard about this," kept ONLY to detect the moment a StockLevelChanged
    // event crosses INTO low stock, never for display. This is not a
    // second Reporting read model (D1/D2 already owns that job in full);
    // it exists purely to stop a LowStock notification firing on every
    // qualifying event once an item is already known to be low.
    public class StockLevelSnapshot : EntityBase
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int QuantityOnHand { get; set; }
        public int ReorderThreshold { get; set; }
    }
}
