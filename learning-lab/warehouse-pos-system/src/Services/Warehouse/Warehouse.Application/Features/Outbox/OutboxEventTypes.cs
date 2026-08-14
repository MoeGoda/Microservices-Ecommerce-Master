namespace Warehouse.Application.Features.Outbox
{
    public static class OutboxEventTypes
    {
        public const string StockLevelChanged = "StockLevelChanged";

        // J — every individual StockTransaction StockAdjustmentStager.Stage()
        // ever writes, fanned out to Reporting alone (unlike
        // StockLevelChanged, Notifications has no use for a raw movement
        // ledger). Additive: StockLevelChanged keeps meaning exactly what
        // it always meant — "here is the resulting balance" — this is a
        // second, independent event carrying the delta/reason/reference
        // that balance snapshot was never meant to carry.
        public const string StockTransactionRecorded = "StockTransactionRecorded";
    }

    public static class OutboxConsumers
    {
        public const string Reporting = "Reporting";
        public const string Notifications = "Notifications";
    }
}
