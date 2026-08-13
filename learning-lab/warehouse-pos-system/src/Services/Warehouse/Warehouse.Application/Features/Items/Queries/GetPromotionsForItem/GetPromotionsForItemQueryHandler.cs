using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Items.Queries.GetPromotionsForItem
{
    public class GetPromotionsForItemQueryHandler : IRequestHandler<GetPromotionsForItemQuery, IEnumerable<PromotionDto>>
    {
        private readonly IPromotionRepository _promotionRepository;

        public GetPromotionsForItemQueryHandler(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public async Task<IEnumerable<PromotionDto>> Handle(GetPromotionsForItemQuery request, CancellationToken cancellationToken)
        {
            var promotions = await _promotionRepository.GetAllForItem(request.ItemId);
            return promotions.Select(PromotionDto.FromEntity);
        }
    }
}
