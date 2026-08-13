using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IProcessedSaleReturnEventRepository
    {
        Task<bool> ExistsForSale(int saleId);
        Task<ProcessedSaleReturnEvent> AddAsync(ProcessedSaleReturnEvent processedSaleReturnEvent);
    }
}
