using FluentValidation;

namespace Identity.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(c => c.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);
            RuleFor(c => c.Email).NotEmpty().EmailAddress();

            // Rules only, no hashing here — FluentValidation's job is to
            // reject bad input before it reaches the handler, not to
            // transform it.
            RuleFor(c => c.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }
    }
}
