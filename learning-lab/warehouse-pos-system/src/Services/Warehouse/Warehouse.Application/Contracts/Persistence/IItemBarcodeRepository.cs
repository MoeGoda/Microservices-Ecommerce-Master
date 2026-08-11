using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IItemBarcodeRepository
    {
        // The one every scan at the POS (Phase C) actually calls — resolves
        // a scanned code straight to its ItemBarcode row *with* the parent
        // Item populated, so a caller gets the item in one round trip
        // without needing to know barcodes and items are different tables.
        Task<ItemBarcode?> GetByBarcode(string barcode);

        Task<bool> BarcodeExists(string barcode);
        Task<IEnumerable<ItemBarcode>> GetByItem(int itemId);

        // Null if the item has no primary barcode yet (allowed — see
        // ItemBarcode.IsPrimary) or doesn't exist.
        Task<ItemBarcode?> GetPrimary(int itemId);

        Task<ItemBarcode> AddAsync(ItemBarcode itemBarcode);
        Task UpdateAsync(ItemBarcode itemBarcode);
    }
}
