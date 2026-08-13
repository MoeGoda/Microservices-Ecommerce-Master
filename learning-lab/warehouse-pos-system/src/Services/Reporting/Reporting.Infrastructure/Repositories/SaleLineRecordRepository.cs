using Microsoft.EntityFrameworkCore;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Repositories
{
    public class SaleLineRecordRepository : ISaleLineRecordRepository
    {
        private readonly ReportingContext _context;

        public SaleLineRecordRepository(ReportingContext context)
        {
            _context = context;
        }

        public async Task<SaleLineRecord> AddAsync(SaleLineRecord record)
        {
            await _context.SaleLineRecords.AddAsync(record);
            return record;
        }

        public async Task<IEnumerable<TopSellingItemDto>> GetTopSellingItems(int take)
        {
            // Max(Sku)/Max(ItemName) rather than "the first line's" — a
            // real GROUP BY has no inherent row order to pick "first"
            // from, and MAX is a universally-translatable aggregate every
            // provider (SqlServer, SQLite) supports identically, unlike
            // reaching for g.First() inside a SQL aggregation. TotalRevenue
            // is summed as double — SQLite has no SUM(decimal) translation
            // (SQL Server sums the real decimal fine) — and cast back after
            // materializing, same tradeoff as SaleRecordRepository.GetSalesByDay.
            var grouped = await _context.SaleLineRecords
                .AsNoTracking()
                .GroupBy(l => l.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    Sku = g.Max(l => l.Sku)!,
                    ItemName = g.Max(l => l.ItemName)!,
                    TotalQuantity = g.Sum(l => l.Quantity),
                    TotalRevenue = g.Sum(l => (double)l.LineTotal),
                })
                .OrderByDescending(g => g.TotalRevenue)
                .Take(take)
                .ToListAsync();

            return grouped.Select(g => new TopSellingItemDto
            {
                ItemId = g.ItemId,
                Sku = g.Sku,
                ItemName = g.ItemName,
                TotalQuantity = g.TotalQuantity,
                TotalRevenue = (decimal)g.TotalRevenue,
            });
        }
    }
}
