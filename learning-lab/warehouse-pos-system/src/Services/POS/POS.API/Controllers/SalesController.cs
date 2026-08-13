using System.Net;
using System.Security.Claims;
using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Sales.Commands.AddSaleLine;
using POS.Application.Features.Sales.Commands.CancelSale;
using POS.Application.Features.Sales.Commands.Checkout;
using POS.Application.Features.Sales.Commands.RemoveSaleLine;
using POS.Application.Features.Sales.Commands.ReturnSale;
using POS.Application.Features.Sales.Commands.StartSale;
using POS.Application.Features.Sales.Queries.GetSaleById;
using POS.Application.Models;

namespace POS.API.Controllers
{
    // Same idiom as Warehouse.API's controllers: every action needs a
    // caller who already holds a token, so [Authorize] sits once at the
    // controller level. F2 adds a Roles restriction at the SAME level —
    // safe here, unlike Warehouse's ItemsController/StockController,
    // because nothing calls INTO SalesController service-to-service; POS
    // only ever calls OUT (to Warehouse/Reporting/Notifications). Every
    // action, including the read-only GetById, requires one of these
    // roles — WarehouseStaff has no business running a register.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Cashier}")]
    public class SalesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SalesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> GetById(int id)
        {
            return Ok(await _mediator.Send(new GetSaleByIdQuery { Id = id }));
        }

        // CashierUserId is taken from the signed-in caller's own token
        // (ClaimTypes.NameIdentifier — the same claim AuthController.Me()
        // already reads) rather than accepted from the request body,
        // following the same "context is authoritative over the body"
        // idiom Warehouse.API's AddBarcode/AddUnit actions established for
        // route-supplied ids. StartSaleCommand's own comment flags
        // LocationId/CashierUserId as trusted-as-given input; this is the
        // one half of that gap this step closes — the cashier can't open a
        // sale claiming to be a different cashier.
        [HttpPost]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> Start([FromBody] StartSaleCommand command)
        {
            command.CashierUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{id:int}/lines")]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> AddLine(int id, [FromBody] AddSaleLineCommand command)
        {
            command.SaleId = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("{id:int}/lines/{lineId:int}")]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> RemoveLine(int id, int lineId)
        {
            return Ok(await _mediator.Send(new RemoveSaleLineCommand { SaleId = id, SaleLineId = lineId }));
        }

        [HttpPost("{id:int}/checkout")]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> Checkout(int id)
        {
            return Ok(await _mediator.Send(new CheckoutCommand { SaleId = id }));
        }

        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> Cancel(int id)
        {
            return Ok(await _mediator.Send(new CancelSaleCommand { SaleId = id }));
        }

        [HttpPost("{id:int}/return")]
        [ProducesResponseType(typeof(SaleDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SaleDto>> Return(int id)
        {
            return Ok(await _mediator.Send(new ReturnSaleCommand { SaleId = id }));
        }
    }
}
