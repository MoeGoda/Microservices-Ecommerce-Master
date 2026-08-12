using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // The product definition — Item's own identity is Sku, not a barcode.
    // A barcode is how the item gets scanned (ItemBarcode, plural, on
    // purpose); a SKU is how the business itself refers to the item
    // internally. Conflating the two would break the moment an item needs
    // a second barcode, which is exactly the case this shape exists to
    // support.
    public class Item : EntityBase
    {
        public string Sku { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        // Every inventory quantity for this item — StockLevel.QuantityOnHand,
        // StockTransaction.QuantityChange — is always expressed in THIS
        // unit. An alternate unit (ItemUnit, e.g. "received by the BOX")
        // gets converted to this base unit the moment it touches
        // inventory; it is never stored in an inventory row directly. That
        // one rule is what keeps "how much do we have" answerable without
        // asking "in what unit, though?" every time.
        public int BaseUnitOfMeasureId { get; set; }
        public UnitOfMeasure BaseUnitOfMeasure { get; set; } = null!;

        // Set only when this Item is itself a retail pack/variant of
        // another Item — e.g. "Water 500ml – Pack of 6" pointing back to
        // "Water 500ml – Single." This is deliberately a SEPARATE Item
        // (its own Sku, barcode, price, StockLevel) rather than an
        // ItemUnit conversion, because a pack like this is independently
        // priced and shelved, not just a different counting unit for the
        // same sellable thing — see ItemUnit for the case where it IS
        // just a counting unit (e.g. "received by the BOX, sold by the
        // PCS, one price per PCS"). Null for a standalone item or a base
        // product with no pack variants.
        public int? ParentItemId { get; set; }
        public Item? ParentItem { get; set; }
    }
}
