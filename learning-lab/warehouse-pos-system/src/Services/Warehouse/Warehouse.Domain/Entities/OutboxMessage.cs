using Warehouse.Domain.Common;

namespace Warehouse.Domain.Entities
{
    // Warehouse's first outbox (D1) — same generic message+delivery
    // shape POS's own outbox generalized into for the same reason: a real
    // second consumer (Reporting) needing to know about stock changes,
    // not a hypothetical one. Each service keeps its own copy of this
    // idiom rather than sharing one — no shared domain assemblies across
    // services, the same rule EntityBase/UnitOfWork already follow.
    public class OutboxMessage : EntityBase
    {
        public string EventType { get; set; } = null!;
        public string PayloadJson { get; set; } = null!;
    }
}
