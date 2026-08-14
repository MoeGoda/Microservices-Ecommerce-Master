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

        public async Task<(IEnumerable<SaleRecord> Records, int TotalCount)> GetPaged(int page, int pageSize)
        {
            var query = _context.SaleRecords
                .AsNoTracking()
                .OrderByDescending(r => r.CompletedAtUtc);

            var totalCount = await query.CountAsync();
            var records = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (records, totalCount);
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

        public async Task<(IEnumerable<SaleRecord> Records, int TotalCount)> GetLedgerPaged(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc)
        {
            var query = _context.SaleRecords.AsNoTracking().AsQueryable();

            if (fromUtc.HasValue)
            {
                query = query.Where(r => r.CompletedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(r => r.CompletedAtUtc <= toUtc.Value);
            }

            query = query.OrderByDescending(r => r.CompletedAtUtc);

            var totalCount = await query.CountAsync();
            var records = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (records, totalCount);
        }

        public async Task<IEnumerable<CashierPerformanceDto>> GetCashierPerformance(DateTime? fromUtc, DateTime? toUtc)
        {
            var query = _context.SaleRecords.AsNoTracking().AsQueryable();

            if (fromUtc.HasValue)
            {
                query = query.Where(r => r.CompletedAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(r => r.CompletedAtUtc <= toUtc.Value);
            }

            // Same double-cast-for-SUM idiom GetSalesByDay uses — see its
            // own comment on why.
            var grouped = await query
                .GroupBy(r => r.CashierUserId)
                .Select(g => new
                {
                    CashierUserId = g.Key,
                    CompletedSaleCount = g.Count(r => r.ReturnedAtUtc == null),
                    ReturnedSaleCount = g.Count(r => r.ReturnedAtUtc != null),
                    TotalRevenue = g.Where(r => r.ReturnedAtUtc == null).Sum(r => (double?)r.Total) ?? 0,
                })
                .OrderByDescending(g => g.TotalRevenue)
                .ToListAsync();

            return grouped.Select(g => new CashierPerformanceDto
            {
                CashierUserId = g.CashierUserId,
                CompletedSaleCount = g.CompletedSaleCount,
                ReturnedSaleCount = g.ReturnedSaleCount,
                TotalRevenue = (decimal)g.TotalRevenue,
                AverageSaleTotal = g.CompletedSaleCount > 0 ? (decimal)g.TotalRevenue / g.CompletedSaleCount : 0,
            });
        }
    }
}
