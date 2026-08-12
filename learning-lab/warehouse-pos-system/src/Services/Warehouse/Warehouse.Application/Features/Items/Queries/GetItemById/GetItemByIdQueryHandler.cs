using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Queries.GetItemById
{
    public class GetItemByIdQueryHandler : IRequestHandler<GetItemByIdQuery, ItemDetailDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemBarcodeRepository _itemBarcodeRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly EffectivePriceResolver _effectivePriceResolver;

        public GetItemByIdQueryHandler(
            IItemRepository itemRepository,
            IItemBarcodeRepository itemBarcodeRepository,
            IItemUnitRepository itemUnitRepository,
            EffectivePriceResolver effectivePriceResolver)
        {
            _itemRepository = itemRepository;
            _itemBarcodeRepository = itemBarcodeRepository;
            _itemUnitRepository = itemUnitRepository;
            _effectivePriceResolver = effectivePriceResolver;
        }

        public async Task<ItemDetailDto> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.Id)
                ?? throw new NotFoundException(nameof(Item), request.Id);

            var barcodes = await _itemBarcodeRepository.GetByItem(item.Id);
            var units = await _itemUnitRepository.GetByItem(item.Id);
            var variants = await _itemRepository.GetVariants(item.Id);

            // Same reasoning as ResolveBarcodeQueryHandler — a single-item
            // detail view is one cheap lookup, unlike GetAllItemsQuery's
            // list of many (see EffectivePriceResolver's own comment on
            // why that one is deliberately left alone for now).
            var effectivePrice = await _effectivePriceResolver.Resolve(item, DateTime.UtcNow);

            return ItemDetailDto.FromEntity(item, barcodes, units, variants, effectivePrice);
        }
    }
}
