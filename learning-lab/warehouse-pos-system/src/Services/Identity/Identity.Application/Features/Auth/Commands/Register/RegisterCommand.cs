using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Register
{
    // A "command" in CQRS: it describes intent to change state and carries
    // exactly the data needed to do it — nothing more. IRequest<AuthResponse>
    // tells MediatR which handler to route this to and what it returns.
    public class RegisterCommand : IRequest<AuthResponse>
    {
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Defaults to the least-privileged role. Only an existing Admin
        // can actually get a non-Cashier value through, though: this
        // command has no idea which of its two callers it's talking to,
        // and shouldn't — AuthController's own two actions (F2) draw that
        // line. Register (anonymous) overwrites this to Cashier
        // unconditionally before sending; CreateUser (Admin-only) sends
        // it through untouched.
        public string Role { get; set; } = Domain.Entities.RoleNames.Cashier;
    }
}
