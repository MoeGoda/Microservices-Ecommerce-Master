using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // The "inbox" side of the outbox pattern POS uses to publish
    // SaleCompleted (Step C3): at-least-once delivery means the SAME
    // event can arrive here more than once (a retry after a timeout that
    // actually succeeded, a crash right after this responds but before
    // POS marks its outbox entry Sent). One row per SaleId ever
    // successfully applied — if it's already here, the handler treats a
    // repeat delivery as a no-op success instead of decrementing stock a
    // second time for the same sale.
    public class ProcessedSaleEvent : EntityBase
    {
        public int SaleId { get; set; }
    }
}
