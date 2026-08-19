using Moq;
using Notifications.Application.Contracts.Persistence;
using Notifications.Application.Features.Notifications.Queries.GetRecentNotifications;
using Notifications.Domain.Entities;
using Xunit;

namespace Notifications.Application.Tests.Features.Notifications.Queries.GetRecentNotifications
{
    public class GetRecentNotificationsQueryHandlerTests
    {
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly GetRecentNotificationsQueryHandler _sut;

        public GetRecentNotificationsQueryHandlerTests()
        {
            _sut = new GetRecentNotificationsQueryHandler(_notificationRepository.Object);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsNewestFirst_PreservesThatOrderInTheMappedResult()
        {
            var newest = new Notification { Id = 3, Type = NotificationType.LowStock, Message = "third", CreatedAt = new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc) };
            var middle = new Notification { Id = 2, Type = NotificationType.SaleReturned, Message = "second", CreatedAt = new DateTime(2026, 8, 19, 11, 0, 0, DateTimeKind.Utc) };
            var oldest = new Notification { Id = 1, Type = NotificationType.SaleCompleted, Message = "first", CreatedAt = new DateTime(2026, 8, 19, 10, 0, 0, DateTimeKind.Utc) };
            _notificationRepository.Setup(r => r.GetRecent(It.IsAny<int>())).ReturnsAsync(new[] { newest, middle, oldest });

            var result = (await _sut.Handle(new GetRecentNotificationsQuery { Take = 20 }, CancellationToken.None)).ToList();

            Assert.Equal(new[] { 3, 2, 1 }, result.Select(dto => dto.Id));
        }

        [Fact]
        public async Task Handle_PassesTheRequestedTakeThroughToTheRepositoryUnchanged()
        {
            _notificationRepository.Setup(r => r.GetRecent(It.IsAny<int>())).ReturnsAsync(Enumerable.Empty<Notification>());

            await _sut.Handle(new GetRecentNotificationsQuery { Take = 7 }, CancellationToken.None);

            _notificationRepository.Verify(r => r.GetRecent(7), Times.Once);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsNothing_ReturnsEmptyResult()
        {
            _notificationRepository.Setup(r => r.GetRecent(It.IsAny<int>())).ReturnsAsync(Enumerable.Empty<Notification>());

            var result = await _sut.Handle(new GetRecentNotificationsQuery { Take = 20 }, CancellationToken.None);

            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_MapsEveryFieldOntoTheDto()
        {
            var createdAt = new DateTime(2026, 8, 19, 9, 30, 0, DateTimeKind.Utc);
            var notification = new Notification { Id = 4, Type = NotificationType.LowStock, Message = "Low stock: Widget", IsRead = true, CreatedAt = createdAt };
            _notificationRepository.Setup(r => r.GetRecent(It.IsAny<int>())).ReturnsAsync(new[] { notification });

            var dto = (await _sut.Handle(new GetRecentNotificationsQuery { Take = 20 }, CancellationToken.None)).Single();

            Assert.Equal(4, dto.Id);
            Assert.Equal(nameof(NotificationType.LowStock), dto.Type);
            Assert.Equal("Low stock: Widget", dto.Message);
            Assert.True(dto.IsRead);
            Assert.Equal(createdAt, dto.CreatedAt);
        }
    }
}
