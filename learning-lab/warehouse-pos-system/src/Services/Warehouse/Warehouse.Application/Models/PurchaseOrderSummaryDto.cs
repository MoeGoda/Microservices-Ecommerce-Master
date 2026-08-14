using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    // The list-view shape — mirrors ItemSummaryDto/ItemDetailDto's own
    // split. LineCount/TotalCost are cheap aggregates a browse list wants
    // without paying for every line's Item/UnitOfMeasure join
    // (PurchaseOrderDetailDto is the full shape, for one order at a time).
    public class PurchaseOrderSummaryDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? OrderedAtUtc { get; set; }
        public int LineCount { get; set; }
        public decimal TotalCost { get; set; }

        // PurchaseOrder.Supplier and .Lines must already be loaded.
        public static PurchaseOrderSummaryDto FromEntity(PurchaseOrder order)
        {
            return new PurchaseOrderSummaryDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier.Name,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                OrderedAtUtc = order.OrderedAtUtc,
                LineCount = order.Lines.Count,
                TotalCost = order.Lines.Sum(l => l.OrderedQuantity * l.UnitCost),
            };
        }
    }
}
