using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class StockLevelDto
    {
        public int ItemId { get; set; }
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public string LocationName { get; set; } = null!;
        public int QuantityOnHand { get; set; }
        public int ReorderThreshold { get; set; }
        public string UnitOfMeasureCode { get; set; } = null!;

        // stockLevel.Location must already be loaded (Include) by whichever
        // repository call produced this.
        public static StockLevelDto FromEntity(StockLevel stockLevel, string unitOfMeasureCode)
        {
            return new StockLevelDto
            {
                ItemId = stockLevel.ItemId,
                LocationId = stockLevel.LocationId,
                LocationCode = stockLevel.Location.Code,
                LocationName = stockLevel.Location.Name,
                QuantityOnHand = stockLevel.QuantityOnHand,
                ReorderThreshold = stockLevel.ReorderThreshold,
                UnitOfMeasureCode = unitOfMeasureCode,
            };
        }
    }
}
