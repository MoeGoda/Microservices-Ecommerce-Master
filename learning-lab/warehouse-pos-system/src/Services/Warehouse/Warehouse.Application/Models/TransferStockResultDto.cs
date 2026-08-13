namespace Warehouse.Application.Models
{
    // Both sides of the move, in one response — a caller showing the
    // result (Angular's Admin Panel) needs the source's new (lower)
    // balance and the destination's new (higher) one at the same time,
    // not just whichever StockLevel a single-location DTO shape could
    // have carried.
    public class TransferStockResultDto
    {
        public StockLevelDto From { get; set; } = null!;
        public StockLevelDto To { get; set; } = null!;
    }
}
