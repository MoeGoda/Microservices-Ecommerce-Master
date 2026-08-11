using Common.Exceptions;
using Identity.Application.Contracts.Infrastructure;
using Identity.Application.Contracts.Persistence;
using Identity.Application.Models;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private const string InvalidCredentialsMessage = "Invalid username or password.";

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByUserName(request.UserName)
                ?? throw new UnauthorizedException(InvalidCredentialsMessage);

            if (!user.IsActive)
            {
                throw new UnauthorizedException(InvalidCredentialsMessage);
            }

            if (!_passwordHasher.Verify(user, user.PasswordHash, request.Password))
            {
                throw new UnauthorizedException(InvalidCredentialsMessage);
            }

            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResponse
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                UserName = user.UserName,
                Role = user.Role.Name
            };
        }
    }
}
