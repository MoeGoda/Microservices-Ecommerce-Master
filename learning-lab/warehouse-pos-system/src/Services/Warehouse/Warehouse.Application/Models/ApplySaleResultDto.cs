namespace Warehouse.Application.Models
{
    public class ApplySaleResultDto
    {
        public int SaleId { get; set; }

        // True when this SaleId had already been recorded as processed —
        // an at-least-once redelivery of the same event, handled as a
        // no-op rather than decrementing stock a second time. POS's
        // outbox dispatcher doesn't need to treat this differently from
        // a fresh success; it's surfaced mainly for observability.
        public bool AlreadyProcessed { get; set; }
    }
}
