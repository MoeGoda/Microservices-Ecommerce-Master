using Common.Exceptions;
using Identity.Application.Contracts.Infrastructure;
using Identity.Application.Contracts.Persistence;
using Identity.Application.Models;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Features.Auth.Commands.Register
{
    public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public RegisterCommandHandler(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPasswordHasher passwordHasher,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
        {
            if (await _userRepository.UserNameExists(request.UserName))
            {
                throw new ConflictException($"Username '{request.UserName}' is already taken.");
            }

            var role = await _roleRepository.GetByName(request.Role)
                ?? throw new NotFoundException(nameof(Role), request.Role);

            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                RoleId = role.Id,
                Role = role
            };

            // The hasher needs the (still password-less) user object because
            // ASP.NET Core's PasswordHasher<T> mixes in per-user salt as part
            // of the PBKDF2 derivation — same password, different users,
            // different hashes.
            user.PasswordHash = _passwordHasher.Hash(user, request.Password);

            var createdUser = await _userRepository.AddAsync(user);

            var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(createdUser);

            return new AuthResponse
            {
                Token = token,
                ExpiresAtUtc = expiresAtUtc,
                UserName = createdUser.UserName,
                Role = role.Name
            };
        }
    }
}
