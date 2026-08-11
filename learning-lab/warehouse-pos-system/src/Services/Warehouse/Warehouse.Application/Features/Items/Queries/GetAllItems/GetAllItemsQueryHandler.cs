using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetAllItems
{
    public class GetAllItemsQueryHandler : IRequestHandler<GetAllItemsQuery, IEnumerable<ItemSummaryDto>>
    {
        private readonly IItemRepository _itemRepository;

        public GetAllItemsQueryHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<IEnumerable<ItemSummaryDto>> Handle(GetAllItemsQuery request, CancellationToken cancellationToken)
        {
            var items = await _itemRepository.GetAll();
            return items.Select(ItemSummaryDto.FromEntity);
        }
    }
}
