using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class ProcessedSaleReturnEventRepository : IProcessedSaleReturnEventRepository
    {
        private readonly WarehouseContext _context;

        public ProcessedSaleReturnEventRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsForSale(int saleId)
        {
            return await _context.ProcessedSaleReturnEvents.AnyAsync(p => p.SaleId == saleId);
        }

        // Stages only — see IUnitOfWork. ApplySaleReturnCommandHandler
        // commits this together with every line's stock change, in one call.
        public async Task<ProcessedSaleReturnEvent> AddAsync(ProcessedSaleReturnEvent processedSaleReturnEvent)
        {
            await _context.ProcessedSaleReturnEvents.AddAsync(processedSaleReturnEvent);
            return processedSaleReturnEvent;
        }
    }
}
