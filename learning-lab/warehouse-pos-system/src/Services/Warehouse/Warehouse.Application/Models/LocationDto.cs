using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class LocationDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;

        public static LocationDto FromEntity(Location location)
        {
            return new LocationDto { Id = location.Id, Code = location.Code, Name = location.Name };
        }
    }
}
