using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // A retail customer a Sale can optionally be attached to. Lives in
    // POS.Domain rather than as a cross-service reference (unlike
    // Sale.LocationId/CashierUserId, which point at Warehouse/Identity
    // rows in a different database) — loyalty points and balance only
    // ever mean anything in the context of a Sale, the same reasoning
    // Suppliers/Purchase Orders live inside Warehouse rather than a
    // separate Purchasing service.
    public class Customer : EntityBase
    {
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Accrued on checkout (see CheckoutSaleCommandHandler) — a real,
        // stored running total, not computed on the fly from sale
        // history, for the same "maintained value, not a live SUM()"
        // reasoning Sale.Total/StockLevel.QuantityOnHand already use.
        public int LoyaltyPoints { get; set; }

        // Store credit / a running tab — a plain signed decimal, positive
        // meaning credit owed TO the customer, negative meaning the
        // customer owes the store. Adjusted only through
        // AdjustCustomerBalanceCommand, an explicit, auditable action —
        // deliberately not a full accounts-receivable ledger with its
        // own transaction history, which is more machinery than this
        // feature set needs.
        public decimal Balance { get; set; }
    }
}
