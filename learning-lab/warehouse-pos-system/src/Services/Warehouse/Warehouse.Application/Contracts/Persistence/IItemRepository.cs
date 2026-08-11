using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IItemRepository
    {
        Task<Item?> GetById(int id);

        // The one every other service will actually call: a barcode scan
        // at the POS or a lookup in the Admin Panel both start here.
        Task<Item?> GetByBarcode(string barcode);

        Task<bool> BarcodeExists(string barcode);
        Task<IEnumerable<Item>> GetAll();
        Task<Item> AddAsync(Item item);
    }
}
