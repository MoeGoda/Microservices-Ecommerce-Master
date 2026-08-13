using Notifications.Domain.Common;

namespace Notifications.Domain.Entities
{
    // Deliberately flat — no FK back to the item/location that triggered
    // it. This is a feed of human-readable messages, not a second read
    // model of POS/Warehouse's data (Reporting already owns that job,
    // D1/D2); Message is rendered once, at ingestion time, from whatever
    // the event carried, and never needs to be joined against anything.
    public class Notification : EntityBase
    {
        public NotificationType Type { get; set; }
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; }

        // Set only for Type == SaleCompleted — purely the dedup key
        // IngestSaleCompletedCommandHandler's existence check relies on, so
        // a repeated delivery of the same sale (POS's outbox is
        // at-least-once) doesn't produce a second toast for the same sale.
        // Never displayed and never joined back to POS for anything.
        // LowStock notifications have no equivalent dedup key — see that
        // handler's own comment for why.
        public int? SourceSaleId { get; set; }
    }
}
