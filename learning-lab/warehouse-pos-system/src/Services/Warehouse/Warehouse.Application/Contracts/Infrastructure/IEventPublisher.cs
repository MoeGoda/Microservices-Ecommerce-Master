namespace Warehouse.Application.Contracts.Infrastructure
{
    // The delivery half of Warehouse's own outbox (D1) — same shape as
    // POS's IEventPublisher (C3/D1), a separate copy rather than a shared
    // one, consistent with this codebase's "no shared domain assemblies
    // across services" rule.
    public interface IEventPublisher
    {
        string ConsumerName { get; }

        Task<EventPublishResult> PublishAsync(string eventType, string payloadJson, CancellationToken cancellationToken);
    }

    public class EventPublishResult
    {
        public bool Success { get; set; }
        public string? Error { get; set; }

        public static EventPublishResult Ok() => new() { Success = true };
        public static EventPublishResult Failed(string error) => new() { Success = false, Error = error };
    }

    // The domain shape of a "StockLevelChanged" event — deliberately just
    // the resulting balance, not a delta: Reporting's StockLevelRecord is
    // a current-snapshot read model (D1), not a ledger, so "what is the
    // level now" is all it ever needs, the same reason StockLevel itself
    // (B1) stores a running balance rather than requiring every reader to
    // sum StockTransaction from scratch.
    public class StockLevelChangedMessage
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int QuantityOnHand { get; set; }
    }
}
