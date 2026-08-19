using Microsoft.Extensions.Logging;
using Moq;
using Notifications.Application.Contracts.Infrastructure;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Features.Ingestion.Commands.IngestStockLevelChanged;
using Notifications.Application.Models;
using Notifications.Domain.Entities;
using Xunit;

namespace Notifications.Application.Tests.Features.Ingestion.Commands.IngestStockLevelChanged
{
    public class IngestStockLevelChangedCommandHandlerTests
    {
        private readonly Mock<IStockLevelSnapshotRepository> _snapshotRepository = new();
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<INotificationPusher> _notificationPusher = new();
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly IngestStockLevelChangedCommandHandler _sut;

        public IngestStockLevelChangedCommandHandlerTests()
        {
            _sut = new IngestStockLevelChangedCommandHandler(
                _snapshotRepository.Object,
                _notificationRepository.Object,
                _unitOfWork.Object,
                _notificationPusher.Object,
                _emailSender.Object,
                new Mock<ILogger<IngestStockLevelChangedCommandHandler>>().Object);

            _notificationRepository
                .Setup(r => r.AddAsync(It.IsAny<Notification>()))
                .ReturnsAsync((Notification n) => n);
        }

        private static IngestStockLevelChangedCommand MakeCommand(int quantityOnHand, int reorderThreshold, int itemId = 1, int locationId = 1) => new()
        {
            ItemId = itemId,
            Sku = "SKU-1",
            ItemName = "Widget",
            LocationId = locationId,
            LocationCode = "LOC-1",
            LocationName = "Main Warehouse",
            QuantityOnHand = quantityOnHand,
            ReorderThreshold = reorderThreshold,
        };

        [Fact]
        public async Task Handle_NewItemLocationArrivesAlreadyLow_CreatesNotification()
        {
            // No snapshot yet counts as "wasn't low" (README E1), so a pair's very first event
            // arriving already at/under threshold is real news, not noise.
            _snapshotRepository.Setup(r => r.GetByItemAndLocation(1, 1)).ReturnsAsync((StockLevelSnapshot?)null);

            await _sut.Handle(MakeCommand(quantityOnHand: 3, reorderThreshold: 5), CancellationToken.None);

            _notificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == NotificationType.LowStock)), Times.Once);
            _snapshotRepository.Verify(r => r.AddAsync(It.Is<StockLevelSnapshot>(s => s.QuantityOnHand == 3 && s.ReorderThreshold == 5)), Times.Once);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NewItemLocationArrivesAboveThreshold_CreatesSnapshotOnlyWithoutNotifying()
        {
            _snapshotRepository.Setup(r => r.GetByItemAndLocation(1, 1)).ReturnsAsync((StockLevelSnapshot?)null);

            var result = await _sut.Handle(MakeCommand(quantityOnHand: 50, reorderThreshold: 5), CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            _snapshotRepository.Verify(r => r.AddAsync(It.IsAny<StockLevelSnapshot>()), Times.Once);
            _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailSender.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_StockCrossesBelowThresholdForTheFirstTime_CreatesNotificationAndSendsEmail()
        {
            var snapshot = new StockLevelSnapshot { ItemId = 1, LocationId = 1, QuantityOnHand = 20, ReorderThreshold = 5 };
            _snapshotRepository.Setup(r => r.GetByItemAndLocation(1, 1)).ReturnsAsync(snapshot);

            await _sut.Handle(MakeCommand(quantityOnHand: 4, reorderThreshold: 5), CancellationToken.None);

            _notificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == NotificationType.LowStock)), Times.Once);
            _snapshotRepository.Verify(r => r.UpdateAsync(It.Is<StockLevelSnapshot>(s => s.QuantityOnHand == 4)), Times.Once);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
            _emailSender.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_StockAlreadyBelowThresholdDropsFurther_DoesNotCreateASecondNotification()
        {
            // The exact bug the README's dedup fix addresses: an item already known to be low
            // must not re-notify just because it keeps falling further below threshold.
            var snapshot = new StockLevelSnapshot { ItemId = 1, LocationId = 1, QuantityOnHand = 8, ReorderThreshold = 10 };
            _snapshotRepository.Setup(r => r.GetByItemAndLocation(1, 1)).ReturnsAsync(snapshot);

            var result = await _sut.Handle(MakeCommand(quantityOnHand: 3, reorderThreshold: 10), CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
            _emailSender.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _snapshotRepository.Verify(r => r.UpdateAsync(It.Is<StockLevelSnapshot>(s => s.QuantityOnHand == 3)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_StockRecoversThenCrossesBelowThresholdAgain_CreatesANewNotificationOnTheSecondCrossing()
        {
            // Same (ItemId, LocationId) pair, two events: recovering above threshold must not
            // notify, but a genuine second crossing back down afterward must.
            var snapshot = new StockLevelSnapshot { ItemId = 1, LocationId = 1, QuantityOnHand = 3, ReorderThreshold = 5 };
            _snapshotRepository.Setup(r => r.GetByItemAndLocation(1, 1)).ReturnsAsync(snapshot);

            var recoveryResult = await _sut.Handle(MakeCommand(quantityOnHand: 30, reorderThreshold: 5), CancellationToken.None);
            Assert.False(recoveryResult.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Never);

            await _sut.Handle(MakeCommand(quantityOnHand: 2, reorderThreshold: 5), CancellationToken.None);

            _notificationRepository.Verify(r => r.AddAsync(It.Is<Notification>(n => n.Type == NotificationType.LowStock)), Times.Once);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EmailSenderThrows_StillReturnsSuccessAndDoesNotPropagate()
        {
            _snapshotRepository.Setup(r => r.GetByItemAndLocation(1, 1)).ReturnsAsync((StockLevelSnapshot?)null);
            _emailSender
                .Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("SMTP relay unreachable"));

            var result = await _sut.Handle(MakeCommand(quantityOnHand: 1, reorderThreshold: 5), CancellationToken.None);

            Assert.False(result.AlreadyProcessed);
            _notificationRepository.Verify(r => r.AddAsync(It.IsAny<Notification>()), Times.Once);
            _notificationPusher.Verify(p => p.PushAsync(It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
