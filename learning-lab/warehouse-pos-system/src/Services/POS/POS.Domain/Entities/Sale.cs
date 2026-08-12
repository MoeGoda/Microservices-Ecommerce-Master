using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // One transaction at a register, from "started" to "paid" (or
    // abandoned). Completing a sale here does NOT reach into Warehouse's
    // database to decrement stock — POS and Warehouse have separate
    // databases, so that has to happen as a separate step reacting to this
    // sale's completion, not as part of the same local transaction. That
    // reaction (a SaleCompleted event + saga) is Step C3; this entity only
    // gets as far as recording that the sale itself is Completed.
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
    }
}
