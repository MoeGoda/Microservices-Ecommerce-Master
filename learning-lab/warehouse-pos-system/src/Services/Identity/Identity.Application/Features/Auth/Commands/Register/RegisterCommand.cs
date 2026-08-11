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
        // should be able to create Admin/Manager/WarehouseStaff accounts —
        // that authorization rule is enforced at the controller (F2), not here.
        public string Role { get; set; } = Domain.Entities.RoleNames.Cashier;
    }
}
