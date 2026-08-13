using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Items;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.ResolveBarcode
{
    public class ResolveBarcodeQueryHandler : IRequestHandler<ResolveBarcodeQuery, ItemDetailDto?>
    {
        private readonly IItemBarcodeRepository _itemBarcodeRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly EffectivePriceResolver _effectivePriceResolver;

        public ResolveBarcodeQueryHandler(
            IItemBarcodeRepository itemBarcodeRepository,
            IItemRepository itemRepository,
            IItemUnitRepository itemUnitRepository,
            EffectivePriceResolver effectivePriceResolver)
        {
            _itemBarcodeRepository = itemBarcodeRepository;
            _itemRepository = itemRepository;
            _itemUnitRepository = itemUnitRepository;
            _effectivePriceResolver = effectivePriceResolver;
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

            // This is the ONE call site a real sale's price actually comes
            // from (AddSaleLineCommandHandler, via IWarehouseCatalogClient,
            // C2) — resolving any active Promotion here, rather than
            // leaving POS to ask about promotions separately, keeps
            // Warehouse the single source of truth for "what does this
            // item actually cost," the same reasoning C2 already applied
            // to stock availability.
            var effectivePrice = await _effectivePriceResolver.Resolve(scanned.Item, DateTime.UtcNow);

            return ItemDetailDto.FromEntity(scanned.Item, barcodes, units, variants, effectivePrice);
        }
    }
}
