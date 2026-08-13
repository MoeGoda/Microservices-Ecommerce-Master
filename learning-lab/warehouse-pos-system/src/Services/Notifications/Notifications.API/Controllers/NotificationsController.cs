using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using Notifications.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using Notifications.Application.Features.Notifications.Queries.GetRecentNotifications;
using Notifications.Application.Models;

namespace Notifications.API.Controllers
{
    // The browser-facing half of this service — routed through the
    // gateway (ocelot.json), unlike EventsController above. The bell
    // dropdown calls GetRecent once on open; everything after that arrives
    // live over the SignalR hub instead of polling this endpoint again.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<NotificationDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetRecent([FromQuery] int take = 20)
        {
            return Ok(await _mediator.Send(new GetRecentNotificationsQuery { Take = take }));
        }

        [HttpPost("{id:int}/read")]
        [ProducesResponseType(typeof(NotificationDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<NotificationDto>> MarkAsRead(int id)
        {
            return Ok(await _mediator.Send(new MarkNotificationAsReadCommand { Id = id }));
        }

        [HttpPost("read-all")]
        [ProducesResponseType(typeof(int), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<int>> MarkAllAsRead()
        {
            return Ok(await _mediator.Send(new MarkAllNotificationsAsReadCommand()));
        }
    }
}
