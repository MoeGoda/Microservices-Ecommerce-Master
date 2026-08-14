using Reporting.Domain.Common;

namespace Reporting.Domain.Entities
{
    // J — one row per StockTransaction Warehouse ever wrote, built from
    // the StockTransactionRecorded event — an append-only ledger,
    // unlike StockLevelRecord's upserted current-snapshot. There is no
    // dedup/idempotency check on insert the way SaleRecord needed one:
    // a repeated delivery of the exact same movement is indistinguishable
    // from two real movements with identical values, so (unlike
    // SaleRecord's unique SaleId) at-least-once delivery here can, in the
    // rare retry case, double-count a movement — a named, accepted gap
    // rather than inventing an artificial per-transaction id Warehouse
    // itself doesn't send.
    public class StockMovementRecord : EntityBase
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityChange { get; set; }
        public string Reason { get; set; } = null!;
        public string? Reference { get; set; }
        public DateTime TransactionAtUtc { get; set; }
    }
}
