using Microsoft.EntityFrameworkCore;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Domain.Entities;
using Warehouse.Infrastructure.Persistence;

namespace Warehouse.Infrastructure.Repositories
{
    public class ProcessedSaleEventRepository : IProcessedSaleEventRepository
    {
        private readonly WarehouseContext _context;

        public ProcessedSaleEventRepository(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsForSale(int saleId)
        {
            return await _context.ProcessedSaleEvents.AnyAsync(p => p.SaleId == saleId);
        }

        // Stages only — see IUnitOfWork. ApplySaleCommandHandler commits
        // this together with every line's stock change, in one call.
        public async Task<ProcessedSaleEvent> AddAsync(ProcessedSaleEvent processedSaleEvent)
        {
            await _context.ProcessedSaleEvents.AddAsync(processedSaleEvent);
            return processedSaleEvent;
        }
    }
}
