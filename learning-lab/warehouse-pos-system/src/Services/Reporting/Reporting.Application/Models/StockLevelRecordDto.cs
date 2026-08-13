using Reporting.Domain.Entities;

namespace Reporting.Application.Models
{
    public class StockLevelRecordDto
    {
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityOnHand { get; set; }
        public int ReorderThreshold { get; set; }
        public DateTime AsOfUtc { get; set; }

        public static StockLevelRecordDto FromEntity(StockLevelRecord record)
        {
            return new StockLevelRecordDto
            {
                ItemId = record.ItemId,
                Sku = record.Sku,
                ItemName = record.ItemName,
                LocationId = record.LocationId,
                LocationCode = record.LocationCode,
                LocationName = record.LocationName,
                QuantityOnHand = record.QuantityOnHand,
                ReorderThreshold = record.ReorderThreshold,
                AsOfUtc = record.AsOfUtc,
            };
        }
    }
}
