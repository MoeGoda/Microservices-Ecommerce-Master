using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface IOutboxRepository
    {
        // Stages only — see IUnitOfWork. CheckoutCommandHandler commits
        // the message and every delivery it fans out to together with
        // the Sale's own Status change.
        Task<OutboxMessage> AddMessageAsync(OutboxMessage message);
        Task<OutboxDelivery> AddDeliveryAsync(OutboxDelivery delivery);
        Task UpdateDeliveryAsync(OutboxDelivery delivery);

        // Everything still Pending, with its OutboxMessage already loaded —
        // the dispatcher needs EventType/PayloadJson for every delivery it
        // processes, not just the delivery row itself. Sent and Failed are
        // both terminal (Failed means retries were already exhausted; see
        // OutboxDispatcher's MaxAttempts), so neither is ever picked up again.
        Task<IEnumerable<OutboxDelivery>> GetPendingDeliveries();
    }
}
