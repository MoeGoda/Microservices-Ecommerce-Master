namespace Reporting.Application.Models
{
    // J — one row per CashierUserId that completed at least one sale in
    // the requested date range. Reporting has no reference to Identity's
    // Users table (no shared domain assemblies, no cross-service join) —
    // the Angular client resolves CashierUserId to a display name itself,
    // via the Users list H's UsersService already exposes to the same
    // Admin/Manager audience this report is restricted to.
    public class CashierPerformanceDto
    {
        public int CashierUserId { get; set; }
        public int CompletedSaleCount { get; set; }
        public int ReturnedSaleCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageSaleTotal { get; set; }
    }
}
