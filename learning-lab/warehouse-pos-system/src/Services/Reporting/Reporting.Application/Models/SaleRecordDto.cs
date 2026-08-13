using Reporting.Domain.Entities;

namespace Reporting.Application.Models
{
    public class SaleRecordDto
    {
        public int SaleId { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal Total { get; set; }
        public DateTime CompletedAtUtc { get; set; }
        public int LineCount { get; set; }

        public static SaleRecordDto FromEntity(SaleRecord record)
        {
            return new SaleRecordDto
            {
                SaleId = record.SaleId,
                LocationId = record.LocationId,
                CashierUserId = record.CashierUserId,
                Total = record.Total,
                CompletedAtUtc = record.CompletedAtUtc,
                LineCount = record.LineCount,
            };
        }
    }
}
