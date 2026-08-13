namespace POS.Domain.Entities
{
    // Whether a Completed sale's stock decrement has actually landed in
    // Warehouse yet — a business-meaningful fact distinct from
    // SaleCompletedOutboxEntry.Status (that's the delivery MECHANISM's own
    // retry bookkeeping; this is "should anyone looking at this sale be
    // worried about it").
    public enum StockSyncStatus
    {
        // The sale is Completed but the outbox entry hasn't been
        // successfully delivered and applied yet — the normal, brief
        // state between checkout and the next dispatch cycle.
        Pending,

        // Warehouse confirmed the decrement was applied (or had already
        // been applied — an idempotent replay counts as Synced too).
        Synced,

        // Delivery/application failed and retries were exhausted. The
        // sale stays Completed — the money was taken, this doesn't
        // un-sell it automatically — but this is the compensating signal:
        // something needs a human to reconcile Warehouse's stock by hand.
        Failed,
    }
}
