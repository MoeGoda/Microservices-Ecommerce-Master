using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Tests.Testing
{
    // An in-memory stand-in for ISaleRecordRepository that actually
    // reproduces the GROUP BY/filter semantics SaleRecordRepository (EF
    // Core) implements against the database — a plain Moq mock can only
    // ever hand back a canned result, so it can't exercise "does ingesting
    // a SaleReturned event change what GetSalesByDay later reports."
    // Deliberately not a copy of the production LINQ (no double-cast-for-
    // SQLite tricks needed here); it only needs to agree with production on
    // the OBSERVABLE result: exclude returned sales, group by completed
    // date, sum totals.
    public class FakeSaleRecordRepository : ISaleRecordRepository
    {
        private readonly List<SaleRecord> _records = new();

        public Task<bool> ExistsForSale(int saleId) => Task.FromResult(_records.Any(r => r.SaleId == saleId));

        public Task<SaleRecord> AddAsync(SaleRecord record)
        {
            _records.Add(record);
            return Task.FromResult(record);
        }

        public Task<(IEnumerable<SaleRecord> Records, int TotalCount)> GetPaged(int page, int pageSize)
        {
            var ordered = _records.OrderByDescending(r => r.CompletedAtUtc).ToList();
            var page_ = ordered.Skip((page - 1) * pageSize).Take(pageSize);
            return Task.FromResult<(IEnumerable<SaleRecord>, int)>((page_, ordered.Count));
        }

        public Task<SaleRecord?> GetBySaleId(int saleId) =>
            Task.FromResult(_records.FirstOrDefault(r => r.SaleId == saleId));

        public Task UpdateAsync(SaleRecord record) => Task.CompletedTask;

        public Task<IEnumerable<SalesByDayDto>> GetSalesByDay()
        {
            var grouped = _records
                .Where(r => r.ReturnedAtUtc == null)
                .GroupBy(r => r.CompletedAtUtc.Date)
                .Select(g => new SalesByDayDto
                {
                    Date = DateOnly.FromDateTime(g.Key),
                    SaleCount = g.Count(),
                    Total = g.Sum(r => r.Total),
                })
                .OrderBy(d => d.Date);
            return Task.FromResult<IEnumerable<SalesByDayDto>>(grouped.ToList());
        }

        public Task<(IEnumerable<SaleRecord> Records, int TotalCount)> GetLedgerPaged(int page, int pageSize, DateTime? fromUtc, DateTime? toUtc)
        {
            var filtered = _records
                .Where(r => !fromUtc.HasValue || r.CompletedAtUtc >= fromUtc.Value)
                .Where(r => !toUtc.HasValue || r.CompletedAtUtc <= toUtc.Value)
                .OrderByDescending(r => r.CompletedAtUtc)
                .ToList();
            var page_ = filtered.Skip((page - 1) * pageSize).Take(pageSize);
            return Task.FromResult<(IEnumerable<SaleRecord>, int)>((page_, filtered.Count));
        }

        public Task<IEnumerable<CashierPerformanceDto>> GetCashierPerformance(DateTime? fromUtc, DateTime? toUtc)
        {
            var filtered = _records
                .Where(r => !fromUtc.HasValue || r.CompletedAtUtc >= fromUtc.Value)
                .Where(r => !toUtc.HasValue || r.CompletedAtUtc <= toUtc.Value);

            var grouped = filtered
                .GroupBy(r => r.CashierUserId)
                .Select(g =>
                {
                    var completed = g.Where(r => r.ReturnedAtUtc == null).ToList();
                    var totalRevenue = completed.Sum(r => r.Total);
                    return new CashierPerformanceDto
                    {
                        CashierUserId = g.Key,
                        CompletedSaleCount = completed.Count,
                        ReturnedSaleCount = g.Count(r => r.ReturnedAtUtc != null),
                        TotalRevenue = totalRevenue,
                        AverageSaleTotal = completed.Count > 0 ? totalRevenue / completed.Count : 0,
                    };
                });
            return Task.FromResult<IEnumerable<CashierPerformanceDto>>(grouped.ToList());
        }
    }
}
