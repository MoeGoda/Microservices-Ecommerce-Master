using Identity.Application.Features.Users.Queries.GetUsers;
using Xunit;

namespace Identity.Application.Tests.Features.Users.Queries.GetUsers
{
    public class GetUsersQueryValidatorTests
    {
        private readonly GetUsersQueryValidator _sut = new();

        [Fact]
        public void Validate_DefaultQuery_IsValid()
        {
            var result = _sut.Validate(new GetUsersQuery());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_PageZero_HasValidationError()
        {
            var result = _sut.Validate(new GetUsersQuery { Page = 0, PageSize = 20 });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUsersQuery.Page));
        }

        [Fact]
        public void Validate_PageSizeZero_HasValidationError()
        {
            var result = _sut.Validate(new GetUsersQuery { Page = 1, PageSize = 0 });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUsersQuery.PageSize));
        }

        [Fact]
        public void Validate_PageSizeOverOneHundred_HasValidationError()
        {
            var result = _sut.Validate(new GetUsersQuery { Page = 1, PageSize = 101 });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetUsersQuery.PageSize));
        }

        [Fact]
        public void Validate_PageSizeAtUpperBound_IsValid()
        {
            var result = _sut.Validate(new GetUsersQuery { Page = 1, PageSize = 100 });

            Assert.True(result.IsValid);
        }
    }
}
