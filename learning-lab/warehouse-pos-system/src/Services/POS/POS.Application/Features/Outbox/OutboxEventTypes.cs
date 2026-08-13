namespace POS.Application.Features.Outbox
{
    // Every EventType string this service ever writes to OutboxMessage —
    // named constants rather than "SaleCompleted" typed out at each call
    // site, the same reasoning as everywhere else values that have to
    // match exactly across files get a single source of truth.
    public static class OutboxEventTypes
    {
        public const string SaleCompleted = "SaleCompleted";

        // ReturnSaleCommand's own event — same consumers as SaleCompleted
        // (Warehouse restocks, Reporting excludes it from revenue,
        // Notifications tells someone), reusing SaleCompletedMessage's
        // shape since a return describes the exact same sale/lines, just
        // with the opposite meaning.
        public const string SaleReturned = "SaleReturned";
    }

    // Every ConsumerName an OutboxDelivery can target — has to match the
    // ConsumerName an IEventPublisher implementation exposes exactly, or
    // OutboxDispatcher has nothing to route that delivery to.
    public static class OutboxConsumers
    {
        public const string Warehouse = "Warehouse";
        public const string Reporting = "Reporting";
        public const string Notifications = "Notifications";
    }
}
