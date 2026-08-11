using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // The append-only audit trail: every event that ever changed a
    // StockLevel's QuantityOnHand, one row per change, never updated or
    // deleted. QuantityChange is always signed (+50 received, -1 sold) —
    // never a separate "increase or decrease" flag — specifically so that
    // summing every QuantityChange for an item+location always equals its
    // current StockLevel.QuantityOnHand. If those two ever disagree,
    // something wrote to one without the other, which is exactly the bug
    // class this ledger exists to make detectable.
    public class StockTransaction : EntityBase
    {
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public int LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public int QuantityChange { get; set; }
        public StockTransactionReason Reason { get; set; }

        // Deliberately a free-form string, not a foreign key: once POS
        // (Phase C) exists, this points at a Sale.Id in a *different
        // service's* database — a real FK constraint can't span that
        // boundary, which is exactly the kind of cross-service reference
        // a microservices system has to represent as data instead of a
        // database-enforced relationship.
        public string? Reference { get; set; }
    }
}
