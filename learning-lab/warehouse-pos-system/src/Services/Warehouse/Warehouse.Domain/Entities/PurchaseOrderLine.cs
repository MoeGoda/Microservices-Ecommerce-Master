using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // I — one item on a PurchaseOrder. OrderedQuantity/ReceivedQuantity
    // are both expressed in THIS line's own UnitOfMeasureId — e.g. a
    // supplier line ordered "10 CARTON" stays in cartons here even after
    // partial receipts, the same way ReceiveStockCommand's own Quantity
    // parameter is expressed in whatever unit the goods arrive in.
    // ReceivePurchaseOrderLineCommandHandler is the only place that
    // converts to the item's base unit — right before it touches
    // StockLevel, never before.
    public class PurchaseOrderLine : EntityBase
    {
        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public int UnitOfMeasureId { get; set; }
        public UnitOfMeasure UnitOfMeasure { get; set; } = null!;

        public decimal OrderedQuantity { get; set; }

        // Never set directly by a request — only
        // ReceivePurchaseOrderLineCommandHandler increments this, by
        // exactly the quantity it just staged into StockLevel/
        // StockTransaction in the same unit of work.
        public decimal ReceivedQuantity { get; set; }

        public decimal UnitCost { get; set; }
    }
}
