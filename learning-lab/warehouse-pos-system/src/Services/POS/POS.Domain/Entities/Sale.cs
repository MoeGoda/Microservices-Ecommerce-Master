using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // One transaction at a register, from "started" to "paid" (or
    // abandoned). Completing a sale here does NOT reach into Warehouse's
    // database to decrement stock — POS and Warehouse have separate
    // databases, so that has to happen as a separate step reacting to this
    // sale's completion, not as part of the same local transaction. That
    // reaction is a SaleCompleted event, published via SaleCompletedOutboxEntry
    // (Step C3) — this entity only gets as far as recording that the sale
    // itself is Completed; StockSyncStatus below is where the OUTCOME of
    // that later, asynchronous reaction lands.
    public class Sale : EntityBase
    {
        // Cross-service references to Warehouse.Location and Identity.User
        // — plain ints, not foreign keys, for the same reason
        // StockTransaction.Reference (Warehouse, B1) isn't one: the actual
        // rows live in a different service's database, and a real FK
        // constraint can't span that boundary.
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }

        public SaleStatus Status { get; set; } = SaleStatus.InProgress;

        // The sum of every SaleLine.LineTotal for this sale. Kept here
        // (rather than computed on every read by summing lines) for the
        // same reason StockLevel.QuantityOnHand is a maintained value
        // rather than a live SUM() — whichever command handler adds or
        // removes a line has to keep this in sync in the same transaction;
        // see AddSaleLineCommandHandler.
        public decimal Total { get; set; }

        public DateTime? CompletedAt { get; set; }

        // Set only when Status transitions to Returned — mirrors
        // CompletedAt's own "when did this state change actually happen"
        // role, one step later in the sale's lifecycle.
        public DateTime? ReturnedAt { get; set; }

        // Set to Pending the moment checkout completes; updated later by
        // the outbox dispatcher once it learns whether Warehouse actually
        // applied the stock decrement. Meaningless (stays at its default)
        // for a sale that's still InProgress or was Cancelled — neither
        // ever gets an outbox entry at all.
        public StockSyncStatus StockSyncStatus { get; set; } = StockSyncStatus.Pending;
    }
}
