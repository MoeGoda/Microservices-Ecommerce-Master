using Warehouse.Domain.Entities;

namespace Warehouse.Application.Models
{
    public class PurchaseOrderDetailDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
        public int CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? OrderedAtUtc { get; set; }
        public List<PurchaseOrderLineDto> Lines { get; set; } = new();

        // PurchaseOrder.Supplier and every Line's Item/UnitOfMeasure must
        // already be loaded.
        public static PurchaseOrderDetailDto FromEntity(PurchaseOrder order)
        {
            return new PurchaseOrderDetailDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierId = order.SupplierId,
                SupplierName = order.Supplier.Name,
                Status = order.Status.ToString(),
                Notes = order.Notes,
                CreatedByUserId = order.CreatedByUserId,
                CreatedAt = order.CreatedAt,
                OrderedAtUtc = order.OrderedAtUtc,
                Lines = order.Lines.Select(PurchaseOrderLineDto.FromEntity).ToList(),
            };
        }
    }
}
