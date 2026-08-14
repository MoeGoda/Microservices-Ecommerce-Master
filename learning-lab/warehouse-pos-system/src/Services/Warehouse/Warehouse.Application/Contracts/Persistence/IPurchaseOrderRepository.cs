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

        // J — every order, unpaged, for the status/aging report — same
        // "small enough to just return it all" reasoning
        // GetSalesByDay/GetLowStock already rely on for their own
        // aggregations; a real deployment's PO count is nowhere near a
        // raw event ledger's scale.
        Task<IEnumerable<PurchaseOrder>> GetAllForAgingReport();

        Task<PurchaseOrder> AddAsync(PurchaseOrder purchaseOrder);

        Task UpdateAsync(PurchaseOrder purchaseOrder);
    }
}
