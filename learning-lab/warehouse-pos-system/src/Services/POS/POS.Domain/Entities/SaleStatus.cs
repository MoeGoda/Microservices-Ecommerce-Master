namespace POS.Domain.Entities
{
    public enum SaleStatus
    {
        // A basket being built at the register — lines can still be
        // added/removed. Nothing outside POS knows this sale exists yet;
        // no Warehouse stock is touched until it's Completed.
        InProgress,

        // Paid and finalized. This is the state transition Step C3 hangs a
        // SaleCompleted event off of, which is what actually decrements
        // Warehouse stock — completing a sale here does NOT touch
        // Warehouse's database directly (see Sale.cs).
        Completed,

        // Abandoned before payment — a customer changed their mind, a
        // cashier started the wrong sale, etc. Deliberately distinct from
        // a POST-completion return/refund, which needs a compensating
        // stock increase and hasn't been designed yet; that's a real,
        // separate feature this enum doesn't try to cover.
        Cancelled,
    }
}
