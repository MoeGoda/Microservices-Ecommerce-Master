using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.CreatePromotion
{
    public class CreatePromotionCommandHandler : IRequestHandler<CreatePromotionCommand, PromotionDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePromotionCommandHandler(
            IItemRepository itemRepository,
            IPromotionRepository promotionRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _promotionRepository = promotionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(CreatePromotionCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var promotion = new Promotion
            {
                ItemId = item.Id,
                Item = item,
                DiscountType = request.DiscountType,
                DiscountValue = request.DiscountValue,
                StartsAtUtc = request.StartsAtUtc,
                EndsAtUtc = request.EndsAtUtc,
            };
            await _promotionRepository.AddAsync(promotion);
            await _unitOfWork.SaveChangesAsync();

            return PromotionDto.FromEntity(promotion);
        }
    }
}
