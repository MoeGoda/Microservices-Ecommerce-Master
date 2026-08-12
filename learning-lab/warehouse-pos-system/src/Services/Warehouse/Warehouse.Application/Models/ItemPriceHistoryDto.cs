using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class ItemPriceHistoryDto
    {
        public int Id { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public DateTime ChangedAtUtc { get; set; }

        public static ItemPriceHistoryDto FromEntity(ItemPriceHistory history)
        {
            return new ItemPriceHistoryDto
            {
                Id = history.Id,
                OldPrice = history.OldPrice,
                NewPrice = history.NewPrice,
                ChangedAtUtc = history.CreatedAt,
            };
        }
    }
}
