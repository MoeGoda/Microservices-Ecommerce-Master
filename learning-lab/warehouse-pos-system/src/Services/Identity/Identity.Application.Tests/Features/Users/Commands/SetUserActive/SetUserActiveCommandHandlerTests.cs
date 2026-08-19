using Common.Exceptions;
using Identity.Application.Contracts.Persistence;
using Identity.Application.Features.Users.Commands.SetUserActive;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.Application.Tests.Features.Users.Commands.SetUserActive
{
    public class SetUserActiveCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly SetUserActiveCommandHandler _sut;

        public SetUserActiveCommandHandlerTests()
        {
            _sut = new SetUserActiveCommandHandler(_userRepository.Object);
        }

        private static User ExistingUser(int id = 5, bool isActive = true) => new()
        {
            Id = id,
            UserName = "jdoe",
            Email = "jdoe@example.com",
            IsActive = isActive,
            Role = new Role { Id = 1, Name = RoleNames.Cashier }
        };

        [Fact]
        public async Task Handle_DeactivatingOwnAccount_ThrowsConflictExceptionWithoutTouchingRepository()
        {
            var command = new SetUserActiveCommand { UserId = 5, IsActive = false, RequestingUserId = 5 };

            await Assert.ThrowsAsync<ConflictException>(() => _sut.Handle(command, CancellationToken.None));

            _userRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
            _userRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_ActivatingOwnAccount_IsAllowedBecauseGuardOnlyBlocksDeactivation()
        {
            var user = ExistingUser(id: 5, isActive: false);
            _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
            var command = new SetUserActiveCommand { UserId = 5, IsActive = true, RequestingUserId = 5 };

            var result = await _sut.Handle(command, CancellationToken.None);

            Assert.True(result.IsActive);
            _userRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_UserDoesNotExist_ThrowsNotFoundException()
        {
            _userRepository.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((User?)null);
            var command = new SetUserActiveCommand { UserId = 99, IsActive = false, RequestingUserId = 1 };

            await Assert.ThrowsAsync<NotFoundException>(() => _sut.Handle(command, CancellationToken.None));

            _userRepository.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task Handle_DeactivatingAnotherUser_PersistsChangeAndReturnsUpdatedDto()
        {
            var user = ExistingUser(id: 5, isActive: true);
            _userRepository.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(user);
            var command = new SetUserActiveCommand { UserId = 5, IsActive = false, RequestingUserId = 1 };

            var result = await _sut.Handle(command, CancellationToken.None);

            Assert.False(result.IsActive);
            Assert.Equal(5, result.Id);
            Assert.False(user.IsActive);
            _userRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
    }
}
