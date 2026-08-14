using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IPurchaseOrderRepository
    {
        // Always with Supplier + Lines (Item, UnitOfMeasure) loaded — a
        // PurchaseOrder is never useful without its lines, unlike Item
        // where barcodes/units/variants are each fetched separately only
        // when a detail view actually needs them.
        Task<PurchaseOrder?> GetById(int id);

        Task<(IEnumerable<PurchaseOrder> Orders, int TotalCount)> GetPaged(int page, int pageSize);

        Task<PurchaseOrder> AddAsync(PurchaseOrder purchaseOrder);

        Task UpdateAsync(PurchaseOrder purchaseOrder);
    }
}
