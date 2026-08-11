using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // The current balance: "this Item has this many units at this
    // Location," right now. This is what a POS barcode scan or an Admin
    // Panel screen actually reads — fast, no math required.
    //
    // It's deliberately a maintained cache, not something computed on
    // read by summing StockTransaction rows: recalculating a SUM() every
    // time anyone checks stock doesn't scale, and it's not what a real
    // warehouse system does either. StockTransaction (below) is the
    // append-only ledger that explains *why* this number is what it is;
    // keeping both in sync — writing a StockTransaction every time this
    // changes, in the same database transaction — is a rule enforced by
    // whichever command handler changes stock (Step B2), not by anything
    // in this entity itself.
    public class StockLevel : EntityBase
    {
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public int LocationId { get; set; }
        public Location Location { get; set; } = null!;

        public int QuantityOnHand { get; set; }

        // Below this, a LowStockEvent should fire (Step E1) — stored here
        // because it's a property of "this item at this location," not a
        // global constant: the back shelf might reasonably run leaner than
        // the front-of-store display location for the same item.
        public int ReorderThreshold { get; set; }
    }
}
