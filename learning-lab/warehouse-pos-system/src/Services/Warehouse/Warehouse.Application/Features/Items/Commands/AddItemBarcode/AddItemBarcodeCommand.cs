using MediatR;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.AddItemBarcode
{
    // The case that motivated ItemBarcode existing at all: giving an item
    // that already has one barcode a second (or third) one — the
    // manufacturer's own vs. a relabeled supplier variant, say — without
    // creating a new Item or touching its shared StockLevel.
    public class AddItemBarcodeCommand : IRequest<ItemBarcodeDto>
    {
        public int ItemId { get; set; }
        public string Barcode { get; set; } = null!;
        public BarcodeType BarcodeType { get; set; } = BarcodeType.EAN13;

        // If true, demotes the item's current primary barcode (if any) in
        // the same transaction — see the handler. An item is never left
        // with two primaries, even momentarily.
        public bool IsPrimary { get; set; }
    }
}
