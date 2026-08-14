using Microsoft.EntityFrameworkCore;
using Reporting.Application.Contracts.Persistence;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Repositories
{
    public class StockMovementRecordRepository : IStockMovementRecordRepository
    {
        private readonly ReportingContext _context;

        public StockMovementRecordRepository(ReportingContext context)
        {
            _context = context;
        }

        public async Task<StockMovementRecord> AddAsync(StockMovementRecord record)
        {
            await _context.StockMovementRecords.AddAsync(record);
            return record;
        }

        public async Task<(IEnumerable<StockMovementRecord> Records, int TotalCount)> GetPaged(
            int page,
            int pageSize,
            DateTime? fromUtc,
            DateTime? toUtc,
            int? itemId,
            int? locationId)
        {
            var query = _context.StockMovementRecords.AsNoTracking().AsQueryable();

            if (fromUtc.HasValue)
            {
                query = query.Where(m => m.TransactionAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(m => m.TransactionAtUtc <= toUtc.Value);
            }

            if (itemId.HasValue)
            {
                query = query.Where(m => m.ItemId == itemId.Value);
            }

            if (locationId.HasValue)
            {
                query = query.Where(m => m.LocationId == locationId.Value);
            }

            query = query.OrderByDescending(m => m.TransactionAtUtc);

            var totalCount = await query.CountAsync();
            var records = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (records, totalCount);
        }
    }
}
