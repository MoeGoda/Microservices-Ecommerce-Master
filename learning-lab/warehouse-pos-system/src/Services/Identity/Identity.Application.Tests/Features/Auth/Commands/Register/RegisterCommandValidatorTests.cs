using Identity.Application.Features.Auth.Commands.Register;
using Xunit;

namespace Identity.Application.Tests.Features.Auth.Commands.Register
{
    public class RegisterCommandValidatorTests
    {
        private readonly RegisterCommandValidator _sut = new();

        private static RegisterCommand ValidCommand() => new()
        {
            UserName = "jdoe",
            Email = "jdoe@example.com",
            Password = "Str0ngPass!",
            FirstName = "John",
            LastName = "Doe"
        };

        [Fact]
        public void Validate_ValidCommand_IsValid()
        {
            var result = _sut.Validate(ValidCommand());

            Assert.True(result.IsValid);
        }

        [Theory]
        [InlineData("")]
        [InlineData("ab")]
        public void Validate_UserNameEmptyOrTooShort_HasValidationError(string userName)
        {
            var command = ValidCommand();
            command.UserName = userName;

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.UserName));
        }

        [Theory]
        [InlineData("not-an-email")]
        [InlineData("")]
        public void Validate_InvalidEmail_HasValidationError(string email)
        {
            var command = ValidCommand();
            command.Email = email;

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Email));
        }

        [Fact]
        public void Validate_PasswordMissingUppercase_HasValidationError()
        {
            var command = ValidCommand();
            command.Password = "str0ngpass!";

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
        }

        [Fact]
        public void Validate_PasswordMissingLowercase_HasValidationError()
        {
            var command = ValidCommand();
            command.Password = "STR0NGPASS!";

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
        }

        [Fact]
        public void Validate_PasswordMissingDigit_HasValidationError()
        {
            var command = ValidCommand();
            command.Password = "StrongPass!";

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
        }

        [Fact]
        public void Validate_PasswordShorterThanEightChars_HasValidationError()
        {
            var command = ValidCommand();
            command.Password = "Str0ng!";

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
        }

        [Fact]
        public void Validate_PasswordLongerThanMax_HasValidationError()
        {
            var command = ValidCommand();
            command.Password = "Str0ng!" + new string('a', 100);

            var result = _sut.Validate(command);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterCommand.Password));
        }
    }
}
