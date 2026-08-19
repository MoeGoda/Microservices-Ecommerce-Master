using Common.Exceptions;
using Identity.Application.Contracts.Infrastructure;
using Identity.Application.Contracts.Persistence;
using Identity.Application.Features.Auth.Commands.Register;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.Application.Tests.Features.Auth.Commands.Register
{
    public class RegisterCommandHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<IRoleRepository> _roleRepository = new();
        private readonly Mock<IPasswordHasher> _passwordHasher = new();
        private readonly Mock<IJwtTokenGenerator> _jwtTokenGenerator = new();
        private readonly RegisterCommandHandler _sut;

        public RegisterCommandHandlerTests()
        {
            _sut = new RegisterCommandHandler(
                _userRepository.Object,
                _roleRepository.Object,
                _passwordHasher.Object,
                _jwtTokenGenerator.Object);
        }

        private static RegisterCommand ValidCommand(string role = RoleNames.Cashier) => new()
        {
            UserName = "jdoe",
            Email = "jdoe@example.com",
            Password = "Str0ngPass!",
            FirstName = "John",
            LastName = "Doe",
            Role = role
        };

        [Fact]
        public async Task Handle_UserNameAlreadyTaken_ThrowsConflictException()
        {
            _userRepository.Setup(r => r.UserNameExists("jdoe")).ReturnsAsync(true);

            await Assert.ThrowsAsync<ConflictException>(() => _sut.Handle(ValidCommand(), CancellationToken.None));

            _roleRepository.Verify(r => r.GetByName(It.IsAny<string>()), Times.Never);
            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_RoleDoesNotExist_ThrowsNotFoundException()
        {
            _userRepository.Setup(r => r.UserNameExists("jdoe")).ReturnsAsync(false);
            _roleRepository.Setup(r => r.GetByName("NotARole")).ReturnsAsync((Role?)null);

            await Assert.ThrowsAsync<NotFoundException>(
                () => _sut.Handle(ValidCommand(role: "NotARole"), CancellationToken.None));

            _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidRequest_HashesPasswordAndReturnsSignedToken()
        {
            var role = new Role { Id = 3, Name = RoleNames.Cashier };
            _userRepository.Setup(r => r.UserNameExists("jdoe")).ReturnsAsync(false);
            _roleRepository.Setup(r => r.GetByName(RoleNames.Cashier)).ReturnsAsync(role);
            _passwordHasher.Setup(h => h.Hash(It.IsAny<User>(), "Str0ngPass!")).Returns("hashed-value");
            _userRepository.Setup(r => r.AddAsync(It.IsAny<User>()))
                .ReturnsAsync((User u) => { u.Id = 42; return u; });
            var expiry = DateTime.UtcNow.AddHours(1);
            _jwtTokenGenerator.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns(("signed-jwt", expiry));

            var result = await _sut.Handle(ValidCommand(), CancellationToken.None);

            Assert.Equal("signed-jwt", result.Token);
            Assert.Equal(expiry, result.ExpiresAtUtc);
            Assert.Equal("jdoe", result.UserName);
            Assert.Equal(RoleNames.Cashier, result.Role);
            _userRepository.Verify(r => r.AddAsync(It.Is<User>(u =>
                u.UserName == "jdoe" && u.RoleId == 3 && u.PasswordHash == "hashed-value")), Times.Once);
        }

        // The Application layer itself does not second-guess the requested
        // Role — that boundary is drawn one layer up, in AuthController (F2):
        // the anonymous /register action overwrites command.Role to Cashier
        // before this handler ever sees it, while /create-user only reaches
        // here after an [Authorize(Roles = Admin)] check. A handler-level
        // test can only document that trust boundary, not enforce it.
        [Fact]
        public async Task Handle_RoleFieldSetToAdmin_PersistsAdminRoleBecauseControllerIsTheOnlyGuard()
        {
            var adminRole = new Role { Id = 1, Name = RoleNames.Admin };
            _userRepository.Setup(r => r.UserNameExists("jdoe")).ReturnsAsync(false);
            _roleRepository.Setup(r => r.GetByName(RoleNames.Admin)).ReturnsAsync(adminRole);
            _passwordHasher.Setup(h => h.Hash(It.IsAny<User>(), It.IsAny<string>())).Returns("hashed-value");
            _userRepository.Setup(r => r.AddAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
            _jwtTokenGenerator.Setup(j => j.GenerateToken(It.IsAny<User>()))
                .Returns(("signed-jwt", DateTime.UtcNow));

            var result = await _sut.Handle(ValidCommand(role: RoleNames.Admin), CancellationToken.None);

            Assert.Equal(RoleNames.Admin, result.Role);
            _userRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.RoleId == 1)), Times.Once);
        }
    }
}
