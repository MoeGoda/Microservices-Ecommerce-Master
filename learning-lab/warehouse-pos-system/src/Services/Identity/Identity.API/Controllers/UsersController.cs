using Common.Pagination;
using Identity.Application.Features.Users.Commands.SetUserActive;
using Identity.Application.Features.Users.Queries.GetUsers;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace Identity.API.Controllers
{
    // H — separate from AuthController on purpose: Auth is about proving
    // who you are (login/register/me); this is about an Admin managing
    // OTHER accounts, a different concern with its own [Authorize]
    // requirement. AuthController's create-user action stays exactly
    // where it is (its route is already load-bearing — ocelot.json,
    // the Angular client once H's UI calls it) rather than moving it
    // here just for tidiness.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = RoleNames.Admin)]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<UserDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResult<UserDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _mediator.Send(new GetUsersQuery { Page = page, PageSize = pageSize }));
        }

        [HttpPost("{id:int}/active")]
        [ProducesResponseType(typeof(UserDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<UserDto>> SetActive(int id, [FromBody] SetUserActiveRequest body)
        {
            var requestingUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _mediator.Send(new SetUserActiveCommand { UserId = id, IsActive = body.IsActive, RequestingUserId = requestingUserId }));
        }
    }

    public class SetUserActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
