using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // A barcode is how an item gets *scanned* — it is not the item's
    // identity (that's Item.Sku). One item can carry several of these (a
    // manufacturer barcode, a supplier's own barcode, a different
    // pack-size barcode that still sells as the same catalog item) and
    // every one of them resolves to the same Item, and therefore the same
    // shared StockLevel — scanning any of them is scanning "this item,"
    // full stop. What used to be a single Item.Barcode string is this
    // table instead, precisely so an item isn't limited to one.
    public class ItemBarcode : EntityBase
    {
        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public string Barcode { get; set; } = null!;
        public BarcodeType BarcodeType { get; set; } = BarcodeType.EAN13;

        // At most one primary barcode per item (enforced by a filtered
        // unique index — see WarehouseContext) — the one shown by default
        // on a receipt or label when an item has several.
        public bool IsPrimary { get; set; }
    }
}
