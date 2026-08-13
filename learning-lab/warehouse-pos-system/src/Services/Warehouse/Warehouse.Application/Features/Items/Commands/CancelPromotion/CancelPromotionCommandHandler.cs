using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.CancelPromotion
{
    public class CancelPromotionCommandHandler : IRequestHandler<CancelPromotionCommand, PromotionDto>
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CancelPromotionCommandHandler(IPromotionRepository promotionRepository, IUnitOfWork unitOfWork)
        {
            _promotionRepository = promotionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PromotionDto> Handle(CancelPromotionCommand request, CancellationToken cancellationToken)
        {
            var promotion = await _promotionRepository.GetById(request.PromotionId);
            if (promotion is null || promotion.ItemId != request.ItemId)
            {
                throw new NotFoundException(nameof(Promotion), request.PromotionId);
            }

            if (promotion.IsCancelled)
            {
                throw new ConflictException($"Promotion {promotion.Id} is already cancelled.");
            }

            promotion.IsCancelled = true;
            await _promotionRepository.UpdateAsync(promotion);
            await _unitOfWork.SaveChangesAsync();

            return PromotionDto.FromEntity(promotion);
        }
    }
}
