using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // I — adapted from the PDF's PO -> Receipt pattern, with the
    // telecom-specific concepts (CSO/project/customer/AWB) dropped; a
    // retail warehouse PO only needs a supplier, a set of ordered lines,
    // and how much of each has actually arrived.
    public class PurchaseOrder : EntityBase
    {
        // Assigned once, right after the first save gives this row its
        // real Id ($"PO-{Id:D6}") — see CreatePurchaseOrderCommandHandler.
        // A human-facing order number, not a second primary key.
        public string OrderNumber { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;
        public string? Notes { get; set; }

        // Cross-service reference to Identity's User — deliberately a
        // plain int with no FK, same reasoning as POS's own
        // Sale.CashierUserId: Identity's Users table lives in a different
        // service's database, so a real foreign-key constraint can't
        // reach it anyway. Set by the controller from the caller's own
        // JWT claim, never trusted from the request body.
        public int CreatedByUserId { get; set; }

        public DateTime? OrderedAtUtc { get; set; }

        public List<PurchaseOrderLine> Lines { get; set; } = new();
    }
}
