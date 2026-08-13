using POS.Domain.Common;

namespace POS.Domain.Entities
{
    // C3's own SaleCompletedOutboxEntry named this exact next step: "a
    // generic outbox is the natural next step the moment a SECOND event
    // type shows up" — D1's Reporting service is that second consumer.
    // This is the event itself, immutable once written: what happened,
    // as a JSON payload only the consumers who understand EventType ever
    // deserialize. It carries no delivery state of its own — see
    // OutboxDelivery for that, since "did Warehouse get this" and "did
    // Reporting get this" are genuinely independent outcomes for the
    // SAME event, not one shared status.
    public class OutboxMessage : EntityBase
    {
        public string EventType { get; set; } = null!;
        public string PayloadJson { get; set; } = null!;
    }
}
