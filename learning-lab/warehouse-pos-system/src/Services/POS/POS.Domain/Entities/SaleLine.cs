using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // One item on a sale. Sku/ItemName/UnitPrice are a SNAPSHOT taken at
    // the moment the line was added, not a live read of Warehouse's
    // current data — deliberately, because a completed sale is a
    // historical record. If Warehouse's price or name for this item
    // changes next week, last week's receipt must not silently change
    // with it; only ItemId is kept as a reference for anything that later
    // needs to trace back to the catalog (e.g. C3's stock decrement).
    public class SaleLine : EntityBase
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; } = null!;

        // Cross-service reference to Warehouse.Item — plain int, same
        // reasoning as Sale.LocationId/CashierUserId above.
        public int ItemId { get; set; }

        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public decimal UnitPrice { get; set; }

        // Always in the item's Warehouse base unit — a barcode scan at the
        // register resolves to one unit of whatever Warehouse considers
        // "the base unit" for that item (see Warehouse's Item.BaseUnitOfMeasure,
        // B1). That's what keeps this number directly usable as-is when
        // C3 eventually calls Warehouse's AdjustStockCommand: no unit
        // conversion needed on this side, because none was introduced here.
        public int Quantity { get; set; }

        // UnitPrice * Quantity, stored rather than computed — like
        // UnitPrice itself, this is part of the historical snapshot, not
        // a value that should silently drift if the math or the price
        // context ever changes later.
        public decimal LineTotal { get; set; }
    }
}
