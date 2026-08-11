using MediatR;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.CreateItem
{
    // Every item needs at least one barcode to be scannable at all, so
    // creating one asks for its first barcode right here rather than
    // forcing a separate "now add a barcode" call immediately after —
    // that first barcode is always IsPrimary; use AddItemBarcodeCommand
    // for every barcode after this one.
    public class CreateItemCommand : IRequest<ItemDetailDto>
    {
        public string Sku { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int CategoryId { get; set; }
        public int BaseUnitOfMeasureId { get; set; }

        // Set only when this item is itself a pack/variant of another item
        // — see Item.ParentItemId.
        public int? ParentItemId { get; set; }

        public string Barcode { get; set; } = null!;
        public BarcodeType BarcodeType { get; set; } = BarcodeType.EAN13;
    }
}
