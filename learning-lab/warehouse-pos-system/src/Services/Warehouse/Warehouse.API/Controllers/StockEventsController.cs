using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Warehouse.Application.Features.Stock.Commands.ApplySale;
using Warehouse.Application.Features.Stock.Commands.ApplySaleReturn;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    // Where POS's SaleCompleted outbox dispatcher (Step C3) delivers, not
    // through the Ocelot gateway — this is a service-to-service call, the
    // same reasoning C2 already established for POS calling Warehouse's
    // own catalog/stock endpoints directly. [Authorize] same as every
    // other Warehouse route; the caller presents the "pos-service" token
    // ServiceAuthHandler (POS.Infrastructure, C2) mints.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class StockEventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public StockEventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("sale-completed")]
        [ProducesResponseType(typeof(ApplySaleResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ApplySaleResultDto>> SaleCompleted([FromBody] ApplySaleCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("sale-returned")]
        [ProducesResponseType(typeof(ApplySaleResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ApplySaleResultDto>> SaleReturned([FromBody] ApplySaleReturnCommand command)
        {
            return Ok(await _mediator.Send(command));
        }
    }
}
