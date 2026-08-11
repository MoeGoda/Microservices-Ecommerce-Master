using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Login
{
    public class LoginCommand : IRequest<AuthResponse>
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
