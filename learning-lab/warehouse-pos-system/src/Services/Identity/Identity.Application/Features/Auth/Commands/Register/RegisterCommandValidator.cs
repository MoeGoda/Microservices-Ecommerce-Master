using FluentValidation;

namespace Identity.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterCommandValidator()
        {
            RuleFor(c => c.UserName).NotEmpty().MinimumLength(3).MaximumLength(50);

            // F2 — MaximumLength(256) is the standard practical email
            // length cap (RFC 3696's own commentary on RFC 5321); EmailAddress()
            // alone only checks shape, not size.
            RuleFor(c => c.Email).NotEmpty().MaximumLength(256).EmailAddress();

            // Rules only, no hashing here — FluentValidation's job is to
            // reject bad input before it reaches the handler, not to
            // transform it. F2 — MaximumLength(100) alongside the existing
            // complexity rules: PBKDF2's hashing cost scales with input
            // length, so this caps how much work a single request can
            // force the server to do, the same reasoning as the
            // string-length caps every other validator in this project
            // already applies.
            RuleFor(c => c.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(100)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }
    }
}
