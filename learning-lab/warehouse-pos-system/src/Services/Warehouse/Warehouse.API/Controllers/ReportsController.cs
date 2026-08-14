using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Warehouse.Application.Features.Reports.Queries.GetInventoryValuation;
using Warehouse.Application.Features.Reports.Queries.GetPurchaseOrderAging;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    // J — Warehouse's own reports: both are live views of data Warehouse
    // already owns (current StockLevel/Item, current PurchaseOrder rows),
    // not event-sourced read models the way Reporting.API's own
    // ReportsController's reports are. Same Admin/Manager-only
    // restriction Reporting.API's ReportsController uses, and the same
    // reasoning — revenue/valuation data isn't something a Cashier or
    // WarehouseStaff account needs to see, and nothing calls into this
    // controller service-to-service.
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

        [HttpGet("inventory-valuation")]
        [ProducesResponseType(typeof(IEnumerable<InventoryValuationLineDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<InventoryValuationLineDto>>> GetInventoryValuation()
        {
            return Ok(await _mediator.Send(new GetInventoryValuationQuery()));
        }

        [HttpGet("purchase-order-aging")]
        [ProducesResponseType(typeof(IEnumerable<PurchaseOrderAgingLineDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<PurchaseOrderAgingLineDto>>> GetPurchaseOrderAging()
        {
            return Ok(await _mediator.Send(new GetPurchaseOrderAgingQuery()));
        }
    }
}
