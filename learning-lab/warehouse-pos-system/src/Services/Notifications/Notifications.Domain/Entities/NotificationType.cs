namespace Notifications.Domain.Entities
{
    // The event types Notifications currently consumes (POS's
    // SaleCompleted/SaleReturned, Warehouse's StockLevelChanged) — see
    // IngestSaleCompletedCommandHandler / IngestSaleReturnedCommandHandler /
    // IngestStockLevelChangedCommandHandler for which one produces which.
    public enum NotificationType
    {
        SaleCompleted,
        LowStock,
        SaleReturned,
    }
}
