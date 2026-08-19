using System.Net;
using System.Security.Claims;
using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.CashDrawer.Commands.CloseCashDrawer;
using POS.Application.Features.CashDrawer.Commands.OpenCashDrawer;
using POS.Application.Features.CashDrawer.Commands.RecordCashMovement;
using POS.Application.Features.CashDrawer.Queries.GetCashDrawerXReport;
using POS.Application.Models;

namespace POS.API.Controllers
{
    // Same Cashier/Manager/Admin set SalesController already uses — a
    // register's own cash drawer is exactly as much "running a register"
    // as ringing up a sale.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Cashier}")]
    public class CashDrawerController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CashDrawerController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // CashierUserId trusted from the token, not the body — same
        // "context is authoritative over the body" idiom
        // SalesController.Start already follows for StartSaleCommand.
        [HttpPost("open")]
        [ProducesResponseType(typeof(CashDrawerSessionDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CashDrawerSessionDto>> Open([FromBody] OpenCashDrawerCommand command)
        {
            command.CashierUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("movements")]
        [ProducesResponseType(typeof(CashMovementDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CashMovementDto>> RecordMovement([FromBody] RecordCashMovementCommand command)
        {
            command.CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _mediator.Send(command));
        }

        [HttpGet("{sessionId:int}/x-report")]
        [ProducesResponseType(typeof(CashDrawerXReportDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CashDrawerXReportDto>> GetXReport(int sessionId)
        {
            return Ok(await _mediator.Send(new GetCashDrawerXReportQuery { SessionId = sessionId }));
        }

        [HttpPost("{sessionId:int}/close")]
        [ProducesResponseType(typeof(CashDrawerSessionDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CashDrawerSessionDto>> Close(int sessionId, [FromBody] CloseCashDrawerCommand command)
        {
            command.SessionId = sessionId;
            return Ok(await _mediator.Send(command));
        }
    }
}
