namespace Warehouse.Domain.Entities
{
    public enum StockTransactionReason
    {
        Received,
        Sale,
        Adjustment,

        // TransferIn/TransferOut below were declared long before anything
        // used them — TransferStockCommand is the first (and only)
        // caller, staging one of each per transfer, one at the source
        // location (TransferOut, negative) and one at the destination
        // (TransferIn, positive), in the same unit of work.
        TransferIn,
        TransferOut,

        // ApplySaleReturnCommand's own reason — a positive
        // StockTransaction restocking what ApplySaleCommand's Sale
        // reason originally decremented. Kept distinct from Adjustment
        // so the audit trail (StockTransaction.Reference already carries
        // "Return of Sale {id}") can be filtered by reason, not just by
        // reading the reference text.
        Return,

        // I — ReceivePurchaseOrderLineCommand's own reason, kept distinct
        // from the plain `Received` a free-text restock
        // (ReceiveStockCommand) uses — same "Return vs. Adjustment"
        // reasoning above: a receipt against a real PO and an ad-hoc
        // receipt are both increases, but only one of them has a supplier
        // and an order number behind it, and the ledger should stay
        // filterable by which is which.
        PurchaseOrderReceived,
    }
}
