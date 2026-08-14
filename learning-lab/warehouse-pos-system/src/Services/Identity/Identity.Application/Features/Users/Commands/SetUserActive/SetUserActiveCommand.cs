using Identity.Application.Features.Users.Queries.GetUsers;
using MediatR;

namespace Identity.Application.Features.Users.Commands.SetUserActive
{
    public class SetUserActiveCommand : IRequest<UserDto>
    {
        public int UserId { get; set; }
        public bool IsActive { get; set; }

        // Set by the controller from the caller's own JWT claim, never
        // from the request body — the same "context is authoritative"
        // idiom AuthController.Register already uses for Role. Lets the
        // handler refuse "deactivate yourself," which would otherwise be
        // a real way for the only Admin account to lock itself out with
        // no other Admin left to undo it.
        public int RequestingUserId { get; set; }
    }
}
