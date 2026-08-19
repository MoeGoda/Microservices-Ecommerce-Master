using Identity.Application.Contracts.Persistence;
using Identity.Application.Features.Users.Queries.GetUsers;
using Identity.Domain.Entities;
using Moq;
using Xunit;

namespace Identity.Application.Tests.Features.Users.Queries.GetUsers
{
    public class GetUsersQueryHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly GetUsersQueryHandler _sut;

        public GetUsersQueryHandlerTests()
        {
            _sut = new GetUsersQueryHandler(_userRepository.Object);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsUsers_MapsEachToDtoAndPreservesPagingMetadata()
        {
            var users = new List<User>
            {
                new()
                {
                    Id = 1, UserName = "admin", Email = "admin@example.com", IsActive = true,
                    Role = new Role { Id = 1, Name = RoleNames.Admin }
                },
                new()
                {
                    Id = 2, UserName = "cashier1", Email = "cashier1@example.com", IsActive = false,
                    Role = new Role { Id = 3, Name = RoleNames.Cashier }
                }
            };
            _userRepository.Setup(r => r.GetAllAsync(2, 10)).ReturnsAsync((users, 25));

            var result = await _sut.Handle(new GetUsersQuery { Page = 2, PageSize = 10 }, CancellationToken.None);

            Assert.Equal(2, result.Items.Count);
            Assert.Equal(2, result.Page);
            Assert.Equal(10, result.PageSize);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(RoleNames.Admin, result.Items[0].Role);
            Assert.Equal(RoleNames.Cashier, result.Items[1].Role);
            Assert.False(result.Items[1].IsActive);
        }

        [Fact]
        public async Task Handle_RepositoryReturnsNoUsers_ReturnsEmptyPagedResult()
        {
            _userRepository.Setup(r => r.GetAllAsync(1, 20)).ReturnsAsync((new List<User>(), 0));

            var result = await _sut.Handle(new GetUsersQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }
    }
}
