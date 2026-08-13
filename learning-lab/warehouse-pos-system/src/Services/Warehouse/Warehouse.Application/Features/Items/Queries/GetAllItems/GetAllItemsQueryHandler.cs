using Common.Pagination;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetAllItems
{
    public class GetAllItemsQueryHandler : IRequestHandler<GetAllItemsQuery, PagedResult<ItemSummaryDto>>
    {
        private readonly IItemRepository _itemRepository;

        public GetAllItemsQueryHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<PagedResult<ItemSummaryDto>> Handle(GetAllItemsQuery request, CancellationToken cancellationToken)
        {
            var (items, totalCount) = await _itemRepository.GetPaged(request.Page, request.PageSize);
            var dtos = items.Select(ItemSummaryDto.FromEntity).ToList();
            return PagedResult<ItemSummaryDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}
