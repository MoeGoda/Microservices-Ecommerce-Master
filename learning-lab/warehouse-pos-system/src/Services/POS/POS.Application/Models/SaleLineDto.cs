using POS.Domain.Entities;

namespace POS.Application.Models
{
    public class SaleLineDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string Sku { get; set; } = null!;
        public string ItemName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public decimal? OriginalUnitPrice { get; set; }
        public int? PromotionId { get; set; }
        public int Quantity { get; set; }
        public decimal LineTotal { get; set; }

        public static SaleLineDto FromEntity(SaleLine line)
        {
            return new SaleLineDto
            {
                Id = line.Id,
                ItemId = line.ItemId,
                Sku = line.Sku,
                ItemName = line.ItemName,
                UnitPrice = line.UnitPrice,
                OriginalUnitPrice = line.OriginalUnitPrice,
                PromotionId = line.PromotionId,
                Quantity = line.Quantity,
                LineTotal = line.LineTotal,
            };
        }
    }
}
