using POS.Domain.Entities;

namespace POS.Application.Models
{
    // Unlike Warehouse's Item (ItemSummaryDto vs ItemDetailDto), Sale gets
    // one DTO shape, always with its lines: there's no "list of many sales
    // without their lines" view anywhere in this design — every use of a
    // Sale is "the one specific sale the register is working on right
    // now," where the lines are the whole point.
    public class SaleDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public string Status { get; set; } = null!;
        public decimal Total { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? ReturnedAt { get; set; }

        // Meaningful only once Status is Completed — see Sale.StockSyncStatus
        // for what Pending/Synced/Failed actually mean.
        public string StockSyncStatus { get; set; } = null!;
        public IReadOnlyList<SaleLineDto> Lines { get; set; } = Array.Empty<SaleLineDto>();

        public static SaleDto FromEntity(Sale sale, IEnumerable<SaleLine> lines)
        {
            return new SaleDto
            {
                Id = sale.Id,
                LocationId = sale.LocationId,
                CashierUserId = sale.CashierUserId,
                Status = sale.Status.ToString(),
                Total = sale.Total,
                CompletedAt = sale.CompletedAt,
                ReturnedAt = sale.ReturnedAt,
                StockSyncStatus = sale.StockSyncStatus.ToString(),
                Lines = lines.Select(SaleLineDto.FromEntity).ToList(),
            };
        }
    }
}
