using Reporting.Domain.Entities;

namespace Reporting.Application.Models
{
    public class StockMovementRecordDto
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityChange { get; set; }
        public string Reason { get; set; } = null!;
        public string? Reference { get; set; }
        public DateTime TransactionAtUtc { get; set; }

        public static StockMovementRecordDto FromEntity(StockMovementRecord record)
        {
            return new StockMovementRecordDto
            {
                ItemId = record.ItemId,
                Sku = record.Sku,
                ItemName = record.ItemName,
                LocationId = record.LocationId,
                LocationCode = record.LocationCode,
                LocationName = record.LocationName,
                QuantityChange = record.QuantityChange,
                Reason = record.Reason,
                Reference = record.Reference,
                TransactionAtUtc = record.TransactionAtUtc,
            };
        }
    }
}
