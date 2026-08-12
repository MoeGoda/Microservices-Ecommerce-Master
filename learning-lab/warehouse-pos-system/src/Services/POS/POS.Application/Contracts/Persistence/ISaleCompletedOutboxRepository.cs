using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface ISaleCompletedOutboxRepository
    {
        Task<SaleCompletedOutboxEntry> AddAsync(SaleCompletedOutboxEntry entry);
        Task UpdateAsync(SaleCompletedOutboxEntry entry);

        // Everything still Pending — Sent and Failed are both terminal
        // (Failed means retries were already exhausted; see
        // SaleCompletedOutboxDispatcher's MaxAttempts), so neither is
        // ever picked up again.
        Task<IEnumerable<SaleCompletedOutboxEntry>> GetPending();
    }
}
