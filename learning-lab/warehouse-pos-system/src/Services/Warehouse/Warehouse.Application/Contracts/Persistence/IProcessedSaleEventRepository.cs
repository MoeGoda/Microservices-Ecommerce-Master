using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IProcessedSaleEventRepository
    {
        Task<bool> ExistsForSale(int saleId);
        Task<ProcessedSaleEvent> AddAsync(ProcessedSaleEvent processedSaleEvent);
    }
}
