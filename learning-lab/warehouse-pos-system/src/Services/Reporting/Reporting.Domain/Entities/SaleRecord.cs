using Reporting.Domain.Common;

namespace Reporting.Domain.Entities
{
    // A denormalized projection of one completed POS sale — built from
    // the SaleCompleted event (POS, C3/D1), not queried live from POS's
    // own database. SaleId is the natural key back to that source of
    // truth and is unique here: IngestSaleCompletedCommandHandler checks
    // for an existing row with the same SaleId before inserting, the
    // same idempotent-receiver idea Warehouse's ProcessedSaleEvent (C3)
    // used for the identical delivery-at-least-once problem, just without
    // a separate inbox table — existence of the SaleRecord itself IS the
    // dedup check, since inserting one is the entire effect of processing
    // this event.
    public class SaleRecord : EntityBase
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal Total { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public int LineCount { get; set; }
    }
}
