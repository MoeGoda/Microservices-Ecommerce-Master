namespace Identity.Domain.Common
{
    // Every entity gets an Id and a CreatedAt "for free" by inheriting this,
    // instead of every entity re-declaring the same two properties.
    public abstract class EntityBase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
