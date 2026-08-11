using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // The thing a barcode scan actually resolves to. Barcode is a plain
    // unique string, not a separate entity: this system has exactly one
    // barcode per item, so a whole extra table (and the join it would
    // need) would model a requirement — multiple barcodes per item, e.g.
    // a supplier's barcode differing from the one on the shelf — that
    // doesn't exist yet. That's the seam where you'd introduce one if it
    // ever does.
    public class Item : EntityBase
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public string Barcode { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; } = true;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
    }
}
