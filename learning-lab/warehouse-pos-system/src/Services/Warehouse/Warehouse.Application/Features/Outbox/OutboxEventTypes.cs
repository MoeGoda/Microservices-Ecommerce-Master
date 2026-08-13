namespace Warehouse.Application.Features.Outbox
{
    public static class OutboxEventTypes
    {
        public const string StockLevelChanged = "StockLevelChanged";
    }

    public static class OutboxConsumers
    {
        public const string Reporting = "Reporting";
        public const string Notifications = "Notifications";
    }
}
