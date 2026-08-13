namespace Notifications.Domain.Common
{
    // Its own copy — not shared with Warehouse/POS/Identity/Reporting, the
    // same "no shared domain assemblies across services" rule every other
    // service already follows.
    public abstract class EntityBase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
