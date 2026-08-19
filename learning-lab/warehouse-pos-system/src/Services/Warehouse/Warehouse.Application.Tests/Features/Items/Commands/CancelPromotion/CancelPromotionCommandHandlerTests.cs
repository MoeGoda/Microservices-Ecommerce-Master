using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Items.Commands.CancelPromotion;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Items.Commands.CancelPromotion
{
    public class CancelPromotionCommandHandlerTests
    {
        private static Promotion BuildPromotion(int itemId, bool isCancelled = false) => new()
        {
            Id = 1,
            ItemId = itemId,
            Item = TestEntities.Item(itemId),
            DiscountType = DiscountType.PercentageOff,
            DiscountValue = 10,
            StartsAtUtc = new DateTime(2026, 1, 1),
            EndsAtUtc = new DateTime(2026, 1, 31),
            IsCancelled = isCancelled,
        };

        [Fact]
        public async Task Handle_ActivePromotionForTheRightItem_CancelsIt()
        {
            var promotionRepository = new Mock<IPromotionRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var promotion = BuildPromotion(itemId: 1);

            promotionRepository.Setup(r => r.GetById(promotion.Id)).ReturnsAsync(promotion);

            var handler = new CancelPromotionCommandHandler(promotionRepository.Object, unitOfWork.Object);
            var result = await handler.Handle(new CancelPromotionCommand { ItemId = 1, PromotionId = promotion.Id }, CancellationToken.None);

            Assert.True(result.IsCancelled);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_AlreadyCancelledPromotion_ThrowsConflictException()
        {
            var promotionRepository = new Mock<IPromotionRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var promotion = BuildPromotion(itemId: 1, isCancelled: true);

            promotionRepository.Setup(r => r.GetById(promotion.Id)).ReturnsAsync(promotion);

            var handler = new CancelPromotionCommandHandler(promotionRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<ConflictException>(
                () => handler.Handle(new CancelPromotionCommand { ItemId = 1, PromotionId = promotion.Id }, CancellationToken.None));

            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_PromotionBelongsToADifferentItem_ThrowsNotFoundException()
        {
            // ItemId is part of the request precisely so a caller can't
            // cancel a promotion by guessing its id while looking at a
            // completely different item's page.
            var promotionRepository = new Mock<IPromotionRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var promotion = BuildPromotion(itemId: 1);

            promotionRepository.Setup(r => r.GetById(promotion.Id)).ReturnsAsync(promotion);

            var handler = new CancelPromotionCommandHandler(promotionRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(new CancelPromotionCommand { ItemId = 999, PromotionId = promotion.Id }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UnknownPromotionId_ThrowsNotFoundException()
        {
            var promotionRepository = new Mock<IPromotionRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            promotionRepository.Setup(r => r.GetById(999)).ReturnsAsync((Promotion?)null);

            var handler = new CancelPromotionCommandHandler(promotionRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(new CancelPromotionCommand { ItemId = 1, PromotionId = 999 }, CancellationToken.None));
        }
    }
}
