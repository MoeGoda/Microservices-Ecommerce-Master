using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IOutboxRepository
    {
        // Stages only — see IUnitOfWork. StockAdjustmentStager commits
        // the message and its delivery together with the StockLevel/
        // StockTransaction change that caused it.
        Task<OutboxMessage> AddMessageAsync(OutboxMessage message);
        Task<OutboxDelivery> AddDeliveryAsync(OutboxDelivery delivery);
        Task UpdateDeliveryAsync(OutboxDelivery delivery);

        Task<IEnumerable<OutboxDelivery>> GetPendingDeliveries();
    }
}
