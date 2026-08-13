using Microsoft.EntityFrameworkCore;
using Reporting.Application.Contracts.Persistence;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Repositories
{
    public class SaleRecordRepository : ISaleRecordRepository
    {
        private readonly ReportingContext _context;

        public SaleRecordRepository(ReportingContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsForSale(int saleId)
        {
            return await _context.SaleRecords.AnyAsync(r => r.SaleId == saleId);
        }

        public async Task<SaleRecord> AddAsync(SaleRecord record)
        {
            await _context.SaleRecords.AddAsync(record);
            return record;
        }

        public async Task<IEnumerable<SaleRecord>> GetAll()
        {
            return await _context.SaleRecords
                .AsNoTracking()
                .OrderByDescending(r => r.CompletedAtUtc)
                .ToListAsync();
        }
    }
}
