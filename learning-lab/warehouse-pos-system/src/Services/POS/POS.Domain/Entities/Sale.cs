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

        // Real FK, not a cross-service int — Customer lives in this same
        // POS database (see Customer.cs). Null for the (still common)
        // case of a walk-in sale with nobody attached.
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // A whole-sale percentage discount, on top of whatever each
        // line's own promotion/manual discount already applied —
        // "Receipt discounts" in the register's action panel. Applied to
        // the line-total sum before tax; see NetTotal's own comment.
        public decimal? ManualReceiptDiscountPercent { get; set; }

        // Toggled by SetTaxExemptCommand — when true, TaxAmount is
        // forced to zero regardless of TaxSettings.RatePercent.
        public bool IsTaxExempt { get; set; }

        // The sum of every SaleLine.LineTotal for this sale, after
        // ManualReceiptDiscountPercent is applied — i.e. what tax is
        // actually computed against. Same "maintained value, not a live
        // SUM()" reasoning as Total below; recomputed alongside it every
        // time a line changes or a discount/tax-exempt flag is set, not
        // just at checkout, so the register shows a live breakdown as
        // items are scanned.
        public decimal NetTotal { get; set; }

        public decimal TaxAmount { get; set; }

        // NetTotal + TaxAmount. Kept here (rather than computed on every
        // read) for the same reason StockLevel.QuantityOnHand is a
        // maintained value rather than a live SUM() — whichever command
        // handler changes a line or a discount has to keep this (and
        // NetTotal/TaxAmount above) in sync in the same transaction; see
        // AddSaleLineCommandHandler.
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
