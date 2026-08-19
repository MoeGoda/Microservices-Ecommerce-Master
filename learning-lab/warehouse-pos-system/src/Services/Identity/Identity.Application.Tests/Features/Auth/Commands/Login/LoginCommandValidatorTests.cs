using Identity.Application.Features.Auth.Commands.Login;
using Xunit;

namespace Identity.Application.Tests.Features.Auth.Commands.Login
{
    public class LoginCommandValidatorTests
    {
        private readonly LoginCommandValidator _sut = new();

        [Fact]
        public void Validate_ValidCommand_IsValid()
        {
            var result = _sut.Validate(new LoginCommand { UserName = "jdoe", Password = "whatever" });

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_EmptyUserName_HasValidationError()
        {
            var result = _sut.Validate(new LoginCommand { UserName = "", Password = "whatever" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.UserName));
        }

        [Fact]
        public void Validate_EmptyPassword_HasValidationError()
        {
            var result = _sut.Validate(new LoginCommand { UserName = "jdoe", Password = "" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
        }

        [Fact]
        public void Validate_UserNameLongerThanMax_HasValidationError()
        {
            var result = _sut.Validate(new LoginCommand { UserName = new string('a', 51), Password = "whatever" });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.UserName));
        }

        [Fact]
        public void Validate_PasswordLongerThanMax_HasValidationError()
        {
            var result = _sut.Validate(new LoginCommand { UserName = "jdoe", Password = new string('a', 201) });

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(LoginCommand.Password));
        }
    }
}
