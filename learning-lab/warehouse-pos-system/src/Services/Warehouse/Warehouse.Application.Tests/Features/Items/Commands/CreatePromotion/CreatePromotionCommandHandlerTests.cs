using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Items.Commands.CreatePromotion;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Items.Commands.CreatePromotion
{
    public class CreatePromotionCommandHandlerTests
    {
        [Fact]
        public async Task Handle_KnownItem_CreatesPromotionScopedToThatItem()
        {
            var itemRepository = new Mock<IItemRepository>();
            var promotionRepository = new Mock<IPromotionRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var item = TestEntities.Item();

            itemRepository.Setup(r => r.GetById(item.Id)).ReturnsAsync(item);
            promotionRepository.Setup(r => r.AddAsync(It.IsAny<Promotion>())).ReturnsAsync((Promotion p) => p);

            var handler = new CreatePromotionCommandHandler(itemRepository.Object, promotionRepository.Object, unitOfWork.Object);
            var command = new CreatePromotionCommand
            {
                ItemId = item.Id,
                DiscountType = DiscountType.PercentageOff,
                DiscountValue = 25,
                StartsAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndsAtUtc = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            };

            var result = await handler.Handle(command, CancellationToken.None);

            Assert.Equal(item.Id, result.ItemId);
            Assert.Equal("PercentageOff", result.DiscountType);
            Assert.False(result.IsCancelled);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_UnknownItem_ThrowsNotFoundException()
        {
            var itemRepository = new Mock<IItemRepository>();
            var promotionRepository = new Mock<IPromotionRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            itemRepository.Setup(r => r.GetById(999)).ReturnsAsync((Item?)null);

            var handler = new CreatePromotionCommandHandler(itemRepository.Object, promotionRepository.Object, unitOfWork.Object);
            var command = new CreatePromotionCommand { ItemId = 999, DiscountType = DiscountType.FixedAmountOff, DiscountValue = 5 };

            await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));

            promotionRepository.Verify(r => r.AddAsync(It.IsAny<Promotion>()), Times.Never);
        }
    }
}
