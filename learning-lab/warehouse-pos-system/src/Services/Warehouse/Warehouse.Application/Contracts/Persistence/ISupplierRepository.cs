using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface ISupplierRepository
    {
        Task<Supplier?> GetById(int id);

        Task<(IEnumerable<Supplier> Suppliers, int TotalCount)> GetPaged(int page, int pageSize);

        Task<Supplier> AddAsync(Supplier supplier);

        // Stages only — see IUnitOfWork. So far only
        // SetSupplierActiveCommand needs this.
        Task UpdateAsync(Supplier supplier);
    }
}
