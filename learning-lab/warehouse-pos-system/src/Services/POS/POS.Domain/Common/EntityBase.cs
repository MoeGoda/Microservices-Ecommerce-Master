namespace POS.Domain.Common
{
    // Its own copy, not a reference to Identity.Domain.Common.EntityBase or
    // Warehouse.Domain.Common.EntityBase — every microservice's Domain
    // project is independently deployable and must not share an assembly
    // with another service's domain layer, even when the base type is
    // structurally identical everywhere. The duplication is the point.
    public abstract class EntityBase
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
