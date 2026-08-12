using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Queries.GetItemVariants
{
    public class GetItemVariantsQueryHandler : IRequestHandler<GetItemVariantsQuery, IEnumerable<ItemSummaryDto>>
    {
        private readonly IItemRepository _itemRepository;

        public GetItemVariantsQueryHandler(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<IEnumerable<ItemSummaryDto>> Handle(GetItemVariantsQuery request, CancellationToken cancellationToken)
        {
            _ = await _itemRepository.GetById(request.ParentItemId)
                ?? throw new NotFoundException(nameof(Item), request.ParentItemId);

            var variants = await _itemRepository.GetVariants(request.ParentItemId);
            return variants.Select(ItemSummaryDto.FromEntity);
        }
    }
}
