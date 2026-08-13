using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Features.Ingestion.Commands.IngestSaleCompleted;
using Notifications.Application.Features.Ingestion.Commands.IngestSaleReturned;
using Notifications.Application.Features.Ingestion.Commands.IngestStockLevelChanged;
using Notifications.Application.Models;

namespace Notifications.API.Controllers
{
    // Mirrors Reporting.API's own EventsController (D1) exactly — a
    // dedicated controller for events arriving from OTHER SERVICES' outbox
    // dispatchers (POS's and Warehouse's new NotificationsEventPublisher),
    // never from a browser, so it isn't routed through the Ocelot gateway
    // at all (see the README for the established reasoning). [Authorize]
    // still applies: a caller with no signed-in user behind it still needs
    // a valid "pos-service"/"warehouse-service" token, the same
    // ServiceAuthHandler idiom every other cross-service call in this
    // system uses.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("sale-completed")]
        [ProducesResponseType(typeof(IngestResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IngestResultDto>> SaleCompleted([FromBody] IngestSaleCompletedCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("sale-returned")]
        [ProducesResponseType(typeof(IngestResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IngestResultDto>> SaleReturned([FromBody] IngestSaleReturnedCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("stock-level-changed")]
        [ProducesResponseType(typeof(IngestResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IngestResultDto>> StockLevelChanged([FromBody] IngestStockLevelChangedCommand command)
        {
            return Ok(await _mediator.Send(command));
        }
    }
}
