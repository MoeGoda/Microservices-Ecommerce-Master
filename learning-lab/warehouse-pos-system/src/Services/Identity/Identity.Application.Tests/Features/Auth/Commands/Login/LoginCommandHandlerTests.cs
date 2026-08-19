using Common.Exceptions;
using Identity.Application.Contracts.Infrastructure;
using Identity.Application.Contracts.Persistence;
using Identity.Application.Features.Auth.Commands.Login;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.Application.Tests.Features.Auth.Commands.Login
{
    public class LoginCommandHandlerTests
    {
        private const string InvalidCredentialsMessage = "Invalid username or password.";

        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
        private readonly LoginCommandHandler _sut;

        public LoginCommandHandlerTests()
        {
            _sut = new LoginCommandHandler(_userRepository.Object, _passwordHasher.Object, _jwtTokenGenerator.Object);
        }

        private static LoginCommand Command() => new() { UserName = "jdoe", Password = "Str0ngPass!" };

        private static User ActiveUser() => new()
        {
            Id = 7,
            UserName = "jdoe",
            PasswordHash = "stored-hash",
            IsActive = true,
            Role = new Role { Id = 3, Name = RoleNames.Cashier }
        };

        [Fact]
        public async Task Handle_UnknownUserName_ThrowsUnauthorizedExceptionWithGenericMessage()
        {
            _userRepository.Setup(r => r.GetByUserName("jdoe")).ReturnsAsync((User?)null);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(
                () => _sut.Handle(Command(), CancellationToken.None));

            Assert.Equal(InvalidCredentialsMessage, ex.Message);
            _passwordHasher.Verify(h => h.Verify(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_InactiveUser_ThrowsUnauthorizedExceptionWithoutCheckingPassword()
        {
            var user = ActiveUser();
            user.IsActive = false;
            _userRepository.Setup(r => r.GetByUserName("jdoe")).ReturnsAsync(user);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(
                () => _sut.Handle(Command(), CancellationToken.None));

            Assert.Equal(InvalidCredentialsMessage, ex.Message);
            _passwordHasher.Verify(h => h.Verify(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WrongPassword_ThrowsUnauthorizedExceptionWithSameMessageAsUnknownUser()
        {
            var user = ActiveUser();
            _userRepository.Setup(r => r.GetByUserName("jdoe")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.Verify(user, "stored-hash", "Str0ngPass!")).Returns(false);

            var ex = await Assert.ThrowsAsync<UnauthorizedException>(
                () => _sut.Handle(Command(), CancellationToken.None));

            // Deliberately the exact same message as the unknown-username
            // case: distinguishing them would let an attacker enumerate
            // valid usernames.
            Assert.Equal(InvalidCredentialsMessage, ex.Message);
        }

        [Fact]
        public async Task Handle_ValidCredentials_ReturnsSignedTokenWithUserRole()
        {
            var user = ActiveUser();
            _userRepository.Setup(r => r.GetByUserName("jdoe")).ReturnsAsync(user);
            _passwordHasher.Setup(h => h.Verify(user, "stored-hash", "Str0ngPass!")).Returns(true);
            var expiry = DateTime.UtcNow.AddHours(1);
            _jwtTokenGenerator.Setup(j => j.GenerateToken(user)).Returns(("signed-jwt", expiry));

            var result = await _sut.Handle(Command(), CancellationToken.None);

            Assert.Equal("signed-jwt", result.Token);
            Assert.Equal(expiry, result.ExpiresAtUtc);
            Assert.Equal("jdoe", result.UserName);
            Assert.Equal(RoleNames.Cashier, result.Role);
        }
    }
}
