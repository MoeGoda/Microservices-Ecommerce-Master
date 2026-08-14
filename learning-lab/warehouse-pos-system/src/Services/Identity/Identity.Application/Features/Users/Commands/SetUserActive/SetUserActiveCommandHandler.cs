using Common.Exceptions;
using Identity.Application.Contracts.Persistence;
using Identity.Application.Features.Users.Queries.GetUsers;
using Identity.Domain.Entities;
using MediatR;

namespace Identity.Application.Features.Users.Commands.SetUserActive
{
    public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand, UserDto>
    {
        private readonly IUserRepository _userRepository;

        public SetUserActiveCommandHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto> Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsActive && request.UserId == request.RequestingUserId)
            {
                throw new ConflictException("You can't deactivate your own account.");
            }

            var user = await _userRepository.GetByIdAsync(request.UserId)
                ?? throw new NotFoundException(nameof(User), request.UserId);

            user.IsActive = request.IsActive;
            await _userRepository.SaveChangesAsync();

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.Name,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
            };
        }
    }
}
