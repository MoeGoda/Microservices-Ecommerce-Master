namespace POS.Domain.Entities
{
    public enum OutboxStatus
    {
        Pending,
        Sent,
        Failed,
    }
}
