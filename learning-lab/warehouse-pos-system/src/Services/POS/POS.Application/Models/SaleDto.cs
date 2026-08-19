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

        // Derived, not stored — "POS-000123" from the sale's own Id.
        // Zero migration cost, same reasoning StockSyncStatus etc. below
        // are mapped straight off the entity rather than duplicated.
        public string DocumentNumber { get; set; } = null!;

        public int LocationId { get; set; }
        public int CashierUserId { get; set; }

        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public string Status { get; set; } = null!;

        public decimal? ManualReceiptDiscountPercent { get; set; }
        public bool IsTaxExempt { get; set; }
        public decimal NetTotal { get; set; }
        public decimal TaxAmount { get; set; }
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
                DocumentNumber = $"POS-{sale.Id:D6}",
                LocationId = sale.LocationId,
                CashierUserId = sale.CashierUserId,
                CustomerId = sale.CustomerId,
                CustomerName = sale.Customer?.Name,
                Status = sale.Status.ToString(),
                ManualReceiptDiscountPercent = sale.ManualReceiptDiscountPercent,
                IsTaxExempt = sale.IsTaxExempt,
                NetTotal = sale.NetTotal,
                TaxAmount = sale.TaxAmount,
                Total = sale.Total,
                CompletedAt = sale.CompletedAt,
                ReturnedAt = sale.ReturnedAt,
                StockSyncStatus = sale.StockSyncStatus.ToString(),
                Lines = lines.Select(SaleLineDto.FromEntity).ToList(),
            };
        }
    }
}
