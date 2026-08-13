namespace Notifications.Application.Models
{
    // Same uniform ingestion-result shape as Reporting's own IngestResultDto
    // (D1) — a repeated delivery of the same event (POS/Warehouse's outbox
    // is at-least-once) is a no-op, not a duplicate Notification, and the
    // caller (a dispatcher retry, ultimately) doesn't need anything richer.
    public class IngestResultDto
    {
        public bool AlreadyProcessed { get; set; }
    }
}
