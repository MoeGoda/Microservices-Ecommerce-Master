using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.ResolveBarcode
{
    public class ResolveBarcodeQueryHandler : IRequestHandler<ResolveBarcodeQuery, ItemDetailDto?>
    {
        private readonly IItemBarcodeRepository _itemBarcodeRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IItemUnitRepository _itemUnitRepository;

        public ResolveBarcodeQueryHandler(
            IItemBarcodeRepository itemBarcodeRepository,
            IItemRepository itemRepository,
            IItemUnitRepository itemUnitRepository)
        {
            _itemBarcodeRepository = itemBarcodeRepository;
            _itemRepository = itemRepository;
            _itemUnitRepository = itemUnitRepository;
        }

        public async Task<ItemDetailDto?> Handle(ResolveBarcodeQuery request, CancellationToken cancellationToken)
        {
            var scanned = await _itemBarcodeRepository.GetByBarcode(request.Barcode);
            if (scanned is null)
            {
                return null;
            }

            var barcodes = await _itemBarcodeRepository.GetByItem(scanned.ItemId);
            var units = await _itemUnitRepository.GetByItem(scanned.ItemId);
            var variants = await _itemRepository.GetVariants(scanned.ItemId);

            return ItemDetailDto.FromEntity(scanned.Item, barcodes, units, variants);
        }
    }
}
