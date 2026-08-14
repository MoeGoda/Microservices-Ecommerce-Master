using System.Net;
using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Features.Reports.Queries.GetLowStock;
using Reporting.Application.Features.Reports.Queries.GetSalesByDay;
using Reporting.Application.Features.Reports.Queries.GetTopSellingItems;
using Reporting.Application.Models;

namespace Reporting.API.Controllers
{
    // The REAL reports (D2), aggregated from the read models D1 built —
    // distinct from ReadModelsController's raw dumps. Routed through the
    // gateway (see ocelot.json) for the Angular dashboard this step adds.
    // F2 — restricted to Admin/Manager: revenue and stock-position data
    // isn't something a Cashier or WarehouseStaff account needs to see.
    // Safe to restrict at the controller level (unlike Warehouse's
    // ItemsController/StockController) because nothing calls into this
    // controller service-to-service — EventsController is the separate,
    // still-bare ingestion endpoint other services actually push events to.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("sales-by-day")]
        [ProducesResponseType(typeof(IEnumerable<SalesByDayDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<SalesByDayDto>>> GetSalesByDay()
        {
            return Ok(await _mediator.Send(new GetSalesByDayQuery()));
        }

        [HttpGet("top-selling-items")]
        [ProducesResponseType(typeof(IEnumerable<TopSellingItemDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<TopSellingItemDto>>> GetTopSellingItems([FromQuery] int take = 10)
        {
            return Ok(await _mediator.Send(new GetTopSellingItemsQuery { Take = take }));
        }

        [HttpGet("low-stock")]
        [ProducesResponseType(typeof(IEnumerable<StockLevelRecordDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<StockLevelRecordDto>>> GetLowStock()
        {
            return Ok(await _mediator.Send(new GetLowStockQuery()));
        }
    }
}
