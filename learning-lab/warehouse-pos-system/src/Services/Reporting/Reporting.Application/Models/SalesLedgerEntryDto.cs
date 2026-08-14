using Reporting.Domain.Entities;

namespace Reporting.Application.Models
{
    // J — the "payment operations by date" report: every completed sale,
    // date-range filterable, WITH its return status shown rather than
    // silently excluded — unlike GetSalesByDay/GetTopSellingItems, which
    // both filter ReturnedAtUtc out because they're revenue totals. A
    // ledger is a record of what happened, a returned sale included.
    public class SalesLedgerEntryDto
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal Total { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public int LineCount { get; set; }
        public DateTime? ReturnedAtUtc { get; set; }

        public static SalesLedgerEntryDto FromEntity(SaleRecord record)
        {
            return new SalesLedgerEntryDto
            {
                SaleId = record.SaleId,
                LocationId = record.LocationId,
                CashierUserId = record.CashierUserId,
                Total = record.Total,
                CompletedAtUtc = record.CompletedAtUtc,
                LineCount = record.LineCount,
                ReturnedAtUtc = record.ReturnedAtUtc,
            };
        }
    }
}
