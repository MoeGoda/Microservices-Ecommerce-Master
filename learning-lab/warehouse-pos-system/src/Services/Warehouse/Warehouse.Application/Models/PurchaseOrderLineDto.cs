using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class PurchaseOrderLineDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string ItemSku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public int UnitOfMeasureId { get; set; }
        public string UnitOfMeasureCode { get; set; } = null!;
        public decimal OrderedQuantity { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public decimal UnitCost { get; set; }

        // Line.Item and Line.UnitOfMeasure must already be loaded.
        public static PurchaseOrderLineDto FromEntity(PurchaseOrderLine line)
        {
            return new PurchaseOrderLineDto
            {
                Id = line.Id,
                ItemId = line.ItemId,
                ItemSku = line.Item.Sku,
                ItemName = line.Item.Name,
                UnitOfMeasureId = line.UnitOfMeasureId,
                UnitOfMeasureCode = line.UnitOfMeasure.Code,
                OrderedQuantity = line.OrderedQuantity,
                ReceivedQuantity = line.ReceivedQuantity,
                UnitCost = line.UnitCost,
            };
        }
    }
}
