using Identity.Application.Features.Auth.Commands.Login;
using Identity.Application.Features.Auth.Commands.Register;
using Identity.Application.Models;
using Identity.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace Identity.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // F2 — the vulnerability RegisterCommand's own comment named
        // explicitly: nothing used to stop an anonymous caller from
        // POSTing { "role": "Admin", ... } here and getting an Admin
        // token back. Same "context is authoritative over the body"
        // idiom SalesController.Start already uses for CashierUserId —
        // an anonymous self-signup is ALWAYS a Cashier account, no matter
        // what the request body claims. Creating an Admin/Manager/
        // WarehouseStaff account is CreateUser's job below, which only an
        // already-authenticated Admin can call.
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterCommand command)
        {
            command.Role = RoleNames.Cashier;

            // The controller doesn't touch _userRepository or _passwordHasher
            // directly — it hands the command to MediatR and gets back a
            // result. Every cross-cutting behaviour (validation, exception
            // logging) already ran by the time this line returns.
            return Ok(await _mediator.Send(command));
        }

        // The other half of the fix above: an Admin creating a
        // Manager/Cashier/WarehouseStaff account for someone else. Reuses
        // RegisterCommand as-is — unlike the public Register action, this
        // one trusts the submitted Role, because only a caller who
        // already holds an Admin token can reach it at all.
        [HttpPost("create-user")]
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(AuthResponse), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthResponse>> CreateUser([FromBody] RegisterCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        // Proves the JWT Bearer middleware is wired correctly: this endpoint
        // is useless on its own, but it's the fastest way to check "did my
        // token actually get accepted and did the claims come through."
        [HttpGet("me")]
        [Authorize]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public IActionResult Me()
        {
            return Ok(new
            {
                UserName = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name,
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }
    }
}
