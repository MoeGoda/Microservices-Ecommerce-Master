using Reporting.Domain.Common;

namespace Reporting.Domain.Entities
{
    // A current-snapshot projection of one (ItemId, LocationId) pair's
    // stock level — built from the StockLevelChanged event (Warehouse,
    // D1). Unlike SaleRecord, this is UPSERTED, not inserted-once: the
    // same pair fires a fresh event every time it changes, and each
    // delivery just overwrites QuantityOnHand/AsOfUtc with the latest
    // known value, the same way StockLevel itself (Warehouse, B1) is a
    // running balance rather than a ledger. That's naturally idempotent —
    // applying the same (ItemId, LocationId, QuantityOnHand) twice leaves
    // the row exactly as it was — so there's no separate dedup check to
    // write here the way SaleRecord needed one.
    public class StockLevelRecord : EntityBase
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime AsOfUtc { get; set; }
    }
}
