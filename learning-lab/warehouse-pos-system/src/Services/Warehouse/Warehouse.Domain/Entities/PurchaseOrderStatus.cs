namespace Warehouse.Domain.Entities
{
    // I — Draft is editable (lines can still be wrong) and cancellable;
    // Ordered means it's been sent to the supplier and is now locked —
    // nothing about "what was ordered" can change, only "how much has
    // arrived" (ReceivePurchaseOrderLineCommand). PartiallyReceived/
    // Received are both derived from every line's ReceivedQuantity vs.
    // OrderedQuantity — no handler ever sets either of these two
    // directly, see PurchaseOrder.RecomputeStatus.
    public enum PurchaseOrderStatus
    {
        Draft,
        Ordered,
        PartiallyReceived,
        Received,
        Cancelled,
    }
}
