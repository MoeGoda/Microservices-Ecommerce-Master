namespace Reporting.Domain.Common
{
    // Reporting's own copy — not shared with Warehouse/POS/Identity, the
    // same "no shared domain assemblies across services" rule every
    // other service already follows.
    public abstract class EntityBase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
