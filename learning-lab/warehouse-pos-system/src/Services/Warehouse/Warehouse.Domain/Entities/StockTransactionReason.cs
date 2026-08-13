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
    }
}
