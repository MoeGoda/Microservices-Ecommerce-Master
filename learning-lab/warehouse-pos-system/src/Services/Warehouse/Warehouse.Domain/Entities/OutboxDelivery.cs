using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // One row per (OutboxMessage, consumer) pair — see POS's own
    // OutboxDelivery for the full reasoning. Warehouse only has one
    // consumer today (Reporting), so this is currently a 1:1 message-to-
    // delivery relationship in practice — but the shape stays the same
    // as POS's for the moment a second Warehouse-side consumer shows up
    // (E1's notifications, most likely), rather than collapsing message
    // and delivery into one row now and having to split them apart later.
    public class OutboxDelivery : EntityBase
    {
        public int OutboxMessageId { get; set; }
        public OutboxMessage OutboxMessage { get; set; } = null!;

        public string ConsumerName { get; set; } = null!;

        public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
        public int Attempts { get; set; }
        public string? LastError { get; set; }
        public DateTime? ProcessedAtUtc { get; set; }
    }
}
