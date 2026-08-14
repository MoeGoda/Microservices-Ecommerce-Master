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
    //
    // Sku/ItemName/LocationCode/LocationName/ReorderThreshold (D2) are
    // denormalized snapshots, not live references — Reporting has no
    // other way to learn an item's name or a location's code, and a
    // "low stock" report showing raw ids instead of "WIDGET-1 below
    // reorder threshold at A1" wouldn't be a report anyone could read.
    public class StockLevelChangedMessage
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityOnHand { get; set; }
        public int ReorderThreshold { get; set; }
    }

    // J — the delta StockLevelChangedMessage never carries: exactly one
    // of these per StockTransaction row StockAdjustmentStager.Stage()
    // writes, so Reporting's stock-movement ledger can show "what
    // happened," not just "what the balance is now."
    public class StockTransactionRecordedMessage
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityChange { get; set; }

        // Serialized as its string name (e.g. "Received",
        // "PurchaseOrderReceived") — Reporting has no reference to
        // Warehouse's StockTransactionReason enum type (no shared domain
        // assemblies across services), so the name itself is the payload.
        public string Reason { get; set; } = null!;
        public string? Reference { get; set; }
        public DateTime TransactionAtUtc { get; set; }
    }
}
