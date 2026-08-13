using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // One row per (OutboxMessage, consumer) pair — the actual unit of
    // delivery/retry tracking. CheckoutCommandHandler writes ONE
    // OutboxMessage for a completed sale but TWO of these (ConsumerName
    // "Warehouse" and "Reporting"), so Warehouse successfully applying
    // the stock decrement and Reporting still retrying its ingestion are
    // tracked — and can fail — completely independently, the same way
    // real message-broker subscriptions each track their own delivery
    // state rather than sharing one flag per message.
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
