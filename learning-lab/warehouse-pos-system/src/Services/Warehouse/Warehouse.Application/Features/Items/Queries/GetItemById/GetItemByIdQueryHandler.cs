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

        public GetItemByIdQueryHandler(
            IItemRepository itemRepository,
            IItemBarcodeRepository itemBarcodeRepository,
            IItemUnitRepository itemUnitRepository)
        {
            _itemRepository = itemRepository;
            _itemBarcodeRepository = itemBarcodeRepository;
            _itemUnitRepository = itemUnitRepository;
        }

        public async Task<ItemDetailDto> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.Id)
                ?? throw new NotFoundException(nameof(Item), request.Id);

            var barcodes = await _itemBarcodeRepository.GetByItem(item.Id);
            var units = await _itemUnitRepository.GetByItem(item.Id);
            var variants = await _itemRepository.GetVariants(item.Id);

            return ItemDetailDto.FromEntity(item, barcodes, units, variants);
        }
    }
}
