using FluentValidation;

namespace Identity.Application.Features.Auth.Commands.Login
{
    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            // F2 — MaximumLength here isn't re-checking password
            // complexity (that's RegisterCommandValidator's job, once, at
            // account-creation time); it's rejecting an absurdly large
            // request body before it ever reaches the handler and gets
            // hashed/compared, the same "reject malformed input before
            // the handler runs" job every validator in this project does.
            // Matches RegisterCommandValidator's own UserName cap.
            RuleFor(c => c.UserName).NotEmpty().MaximumLength(50);
            RuleFor(c => c.Password).NotEmpty().MaximumLength(200);
        }
    }
}
