namespace Notifications.Domain.Entities
{
    // The only two event types Notifications currently consumes (POS's
    // SaleCompleted, Warehouse's StockLevelChanged) — see
    // IngestSaleCompletedCommandHandler / IngestStockLevelChangedCommandHandler
    // for which one produces which.
    public enum NotificationType
    {
        SaleCompleted,
        LowStock,
    }
}
