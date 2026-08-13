using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class PromotionDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string DiscountType { get; set; } = null!;
        public decimal DiscountValue { get; set; }
        public DateTime StartsAtUtc { get; set; }
        public DateTime EndsAtUtc { get; set; }
        public bool IsCancelled { get; set; }

        public static PromotionDto FromEntity(Promotion promotion)
        {
            return new PromotionDto
            {
                Id = promotion.Id,
                ItemId = promotion.ItemId,
                DiscountType = promotion.DiscountType.ToString(),
                DiscountValue = promotion.DiscountValue,
                StartsAtUtc = promotion.StartsAtUtc,
                EndsAtUtc = promotion.EndsAtUtc,
                IsCancelled = promotion.IsCancelled,
            };
        }
    }
}
