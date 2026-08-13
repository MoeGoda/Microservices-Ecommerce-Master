using Microsoft.EntityFrameworkCore;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
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

        public async Task<SaleRecord?> GetBySaleId(int saleId)
        {
            return await _context.SaleRecords.FirstOrDefaultAsync(r => r.SaleId == saleId);
        }

        public Task UpdateAsync(SaleRecord record)
        {
            _context.SaleRecords.Update(record);
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<SalesByDayDto>> GetSalesByDay()
        {
            // Grouping on .Date (translates to the provider's own
            // date-truncation function) keeps the aggregation in the
            // database; DateOnly.FromDateTime happens client-side after
            // materializing, since that conversion itself doesn't need to
            // translate to SQL. SUM(decimal) has no SQLite translation
            // (SQLite has no native decimal type), so the measure is summed
            // as double in SQL and cast back — SQL Server sums the real
            // decimal fine, but this keeps the same query portable across
            // both providers at the cost of double's precision, which is
            // ample for a reporting total.
            // Excludes returned sales — a sale that's been given back
            // shouldn't keep counting toward the day's revenue.
            var grouped = await _context.SaleRecords
                .AsNoTracking()
                .Where(r => r.ReturnedAtUtc == null)
                .GroupBy(r => r.CompletedAtUtc.Date)
                .Select(g => new { Date = g.Key, SaleCount = g.Count(), Total = g.Sum(r => (double)r.Total) })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return grouped.Select(g => new SalesByDayDto
            {
                Date = DateOnly.FromDateTime(g.Date),
                SaleCount = g.SaleCount,
                Total = (decimal)g.Total,
            });
        }
    }
}
