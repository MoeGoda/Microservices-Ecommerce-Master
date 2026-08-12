using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class UnitOfMeasureDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;

        public static UnitOfMeasureDto FromEntity(UnitOfMeasure unitOfMeasure)
        {
            return new UnitOfMeasureDto { Id = unitOfMeasure.Id, Code = unitOfMeasure.Code, Name = unitOfMeasure.Name };
        }
    }
}
