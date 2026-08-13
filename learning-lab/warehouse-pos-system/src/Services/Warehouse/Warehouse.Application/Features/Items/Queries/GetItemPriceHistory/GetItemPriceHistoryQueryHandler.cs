using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Queries.GetItemPriceHistory
{
    public class GetItemPriceHistoryQueryHandler : IRequestHandler<GetItemPriceHistoryQuery, IEnumerable<ItemPriceHistoryDto>>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemPriceHistoryRepository _itemPriceHistoryRepository;

        public GetItemPriceHistoryQueryHandler(IItemRepository itemRepository, IItemPriceHistoryRepository itemPriceHistoryRepository)
        {
            _itemRepository = itemRepository;
            _itemPriceHistoryRepository = itemPriceHistoryRepository;
        }

        public async Task<IEnumerable<ItemPriceHistoryDto>> Handle(GetItemPriceHistoryQuery request, CancellationToken cancellationToken)
        {
            _ = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var history = await _itemPriceHistoryRepository.GetByItem(request.ItemId);
            return history.Select(ItemPriceHistoryDto.FromEntity);
        }
    }
}
