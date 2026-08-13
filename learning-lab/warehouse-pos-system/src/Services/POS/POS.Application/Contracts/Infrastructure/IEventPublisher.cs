namespace POS.Application.Contracts.Infrastructure
{
    // The delivery half of the outbox pattern, generalized (C3 named this
    // exact next step) — OutboxDispatcher picks the IEventPublisher whose
    // ConsumerName matches an OutboxDelivery's ConsumerName and hands it
    // the raw event; each publisher decides how to deserialize
    // PayloadJson and where to send it. WarehouseEventPublisher (ConsumerName
    // "Warehouse") posts to Warehouse.API's StockEventsController exactly
    // as it did before C3's generalization; ReportingEventPublisher
    // (ConsumerName "Reporting", new in D1) posts to Reporting.API instead.
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

    // The domain shape of a "SaleCompleted" event — still a specific,
    // named type (not a generic dictionary) because that's exactly what
    // gets JSON-serialized into OutboxMessage.PayloadJson and deserialized
    // back out by whichever publisher understands EventType == "SaleCompleted".
    // Generalizing the OUTBOX itself doesn't mean every event's payload
    // shape has to become generic too.
    public class SaleCompletedMessage
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal Total { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public IReadOnlyList<SaleCompletedLine> Lines { get; set; } = Array.Empty<SaleCompletedLine>();
    }

    public class SaleCompletedLine
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }
    }
}
