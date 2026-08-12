using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    // The list-view shape — an admin grid or a variant list doesn't need
    // every barcode and unit conversion for every row. ItemDetailDto is the
    // full shape; see it for why the two are kept separate rather than one
    // type with sometimes-empty collections.
    public class ItemSummaryDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public int BaseUnitOfMeasureId { get; set; }
        public string BaseUnitOfMeasureCode { get; set; } = null!;
        public int? ParentItemId { get; set; }

        public static ItemSummaryDto FromEntity(Item item)
        {
            return new ItemSummaryDto
            {
                Id = item.Id,
                Sku = item.Sku,
                Name = item.Name,
                UnitPrice = item.UnitPrice,
                IsActive = item.IsActive,
                CategoryId = item.CategoryId,
                CategoryName = item.Category.Name,
                BaseUnitOfMeasureId = item.BaseUnitOfMeasureId,
                BaseUnitOfMeasureCode = item.BaseUnitOfMeasure.Code,
                ParentItemId = item.ParentItemId,
            };
        }
    }
}
