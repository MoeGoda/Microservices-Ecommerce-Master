using Reporting.Domain.Entities;

namespace Reporting.Application.Models
{
    public class StockLevelRecordDto
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public int QuantityOnHand { get; set; }
        public DateTime AsOfUtc { get; set; }

        public static StockLevelRecordDto FromEntity(StockLevelRecord record)
        {
            return new StockLevelRecordDto
            {
                ItemId = record.ItemId,
                LocationId = record.LocationId,
                QuantityOnHand = record.QuantityOnHand,
                AsOfUtc = record.AsOfUtc,
            };
        }
    }
}
