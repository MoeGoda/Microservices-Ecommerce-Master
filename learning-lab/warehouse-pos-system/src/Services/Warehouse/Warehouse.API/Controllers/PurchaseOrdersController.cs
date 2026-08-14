using Common.Pagination;
using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using Warehouse.Application.Features.PurchaseOrders.Commands.CancelPurchaseOrder;
using Warehouse.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder;
using Warehouse.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrderLine;
using Warehouse.Application.Features.PurchaseOrders.Commands.SubmitPurchaseOrder;
using Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrderById;
using Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PurchaseOrdersController : ControllerBase
    {
        private const string CatalogManagerRoles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}";

        private readonly IMediator _mediator;

        public PurchaseOrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<PurchaseOrderSummaryDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResult<PurchaseOrderSummaryDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _mediator.Send(new GetPurchaseOrdersQuery { Page = page, PageSize = pageSize }));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(PurchaseOrderDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PurchaseOrderDetailDto>> GetById(int id)
        {
            return Ok(await _mediator.Send(new GetPurchaseOrderByIdQuery { Id = id }));
        }

        [HttpPost]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(PurchaseOrderDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PurchaseOrderDetailDto>> Create([FromBody] CreatePurchaseOrderCommand command)
        {
            // Same "context is authoritative over the body" idiom
            // SalesController.Start uses for CashierUserId.
            command.CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{id:int}/submit")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(PurchaseOrderDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PurchaseOrderDetailDto>> Submit(int id)
        {
            return Ok(await _mediator.Send(new SubmitPurchaseOrderCommand { PurchaseOrderId = id }));
        }

        [HttpPost("{id:int}/cancel")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(PurchaseOrderDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PurchaseOrderDetailDto>> Cancel(int id)
        {
            return Ok(await _mediator.Send(new CancelPurchaseOrderCommand { PurchaseOrderId = id }));
        }

        [HttpPost("{id:int}/lines/{lineId:int}/receive")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(PurchaseOrderDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PurchaseOrderDetailDto>> ReceiveLine(int id, int lineId, [FromBody] ReceivePurchaseOrderLineCommand command)
        {
            command.PurchaseOrderId = id;
            command.PurchaseOrderLineId = lineId;
            return Ok(await _mediator.Send(command));
        }
    }
}
