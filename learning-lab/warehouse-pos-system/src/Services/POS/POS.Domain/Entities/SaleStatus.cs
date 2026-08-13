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
        // cashier started the wrong sale, etc. Distinct from Returned
        // below: a Cancelled sale never touched Warehouse's stock in the
        // first place (see Sale.cs), so there's nothing to compensate.
        Cancelled,

        // A completed sale, reversed after the fact — a customer brought
        // the items back. Unlike Cancelled, this DOES need a compensating
        // stock increase (ReturnSaleCommand's own SaleReturned event,
        // mirroring SaleCompleted's decrement in reverse), because the
        // original sale already decremented Warehouse's stock. Only a
        // Completed sale can transition here; an InProgress or Cancelled
        // sale never touched stock, so there's nothing to return.
        Returned,
    }
}
