namespace Warehouse.Domain.Entities
{
    public enum OutboxStatus
    {
        Pending,
        Sent,
        Failed,
    }
}
