using Common.Pagination;
using MediatR;

namespace Identity.Application.Features.Users.Queries.GetUsers
{
    public class GetUsersQuery : IRequest<PagedResult<UserDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
