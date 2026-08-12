using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public static CategoryDto FromEntity(Category category)
        {
            return new CategoryDto { Id = category.Id, Name = category.Name };
        }
    }
}
