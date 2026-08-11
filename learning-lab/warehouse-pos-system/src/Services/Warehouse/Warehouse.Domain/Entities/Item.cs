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
    }
}
