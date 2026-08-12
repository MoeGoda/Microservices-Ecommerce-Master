using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class ItemUnitDto
    {
        public int Id { get; set; }
        public int UnitOfMeasureId { get; set; }
        public string UnitOfMeasureCode { get; set; } = null!;
        public decimal ConversionFactor { get; set; }

        // itemUnit.UnitOfMeasure must already be loaded (Include) by
        // whichever repository call produced this — this mapper doesn't
        // (and, being a plain static method, can't) go fetch it itself.
        public static ItemUnitDto FromEntity(ItemUnit itemUnit)
        {
            return new ItemUnitDto
            {
                Id = itemUnit.Id,
                UnitOfMeasureId = itemUnit.UnitOfMeasureId,
                UnitOfMeasureCode = itemUnit.UnitOfMeasure.Code,
                ConversionFactor = itemUnit.ConversionFactor,
            };
        }
    }
}
