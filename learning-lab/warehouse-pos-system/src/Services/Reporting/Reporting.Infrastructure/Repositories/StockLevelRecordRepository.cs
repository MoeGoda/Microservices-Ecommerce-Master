using Microsoft.EntityFrameworkCore;
using Reporting.Application.Contracts.Persistence;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Repositories
{
    public class StockLevelRecordRepository : IStockLevelRecordRepository
    {
        private readonly ReportingContext _context;

        public StockLevelRecordRepository(ReportingContext context)
        {
            _context = context;
        }

        public async Task<StockLevelRecord?> GetByItemAndLocation(int itemId, int locationId)
        {
            return await _context.StockLevelRecords
                .FirstOrDefaultAsync(r => r.ItemId == itemId && r.LocationId == locationId);
        }

        public async Task<StockLevelRecord> AddAsync(StockLevelRecord record)
        {
            await _context.StockLevelRecords.AddAsync(record);
            return record;
        }

        public Task UpdateAsync(StockLevelRecord record)
        {
            _context.StockLevelRecords.Update(record);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<StockLevelRecord>> GetAll()
        {
            return await _context.StockLevelRecords
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<StockLevelRecord>> GetLowStock()
        {
            return await _context.StockLevelRecords
                .AsNoTracking()
                .Where(r => r.QuantityOnHand <= r.ReorderThreshold)
                .OrderBy(r => r.QuantityOnHand)
                .ToListAsync();
        }
    }
}
