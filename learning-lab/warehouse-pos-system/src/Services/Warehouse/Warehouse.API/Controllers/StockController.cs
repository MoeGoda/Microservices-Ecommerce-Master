using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Warehouse.Application.Features.Stock.Commands.AdjustStock;
using Warehouse.Application.Features.Stock.Commands.ReceiveStock;
using Warehouse.Application.Features.Stock.Commands.TransferStock;
using Warehouse.Application.Features.Stock.Queries.GetStockLevels;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class StockController : ControllerBase
    {
        // Same set as ItemsController.CatalogManagerRoles — Cashier only
        // ever reads stock levels (via POS's C2 service-to-service call,
        // GetByItem below), never mutates them.
        private const string CatalogManagerRoles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}";

        private readonly IMediator _mediator;

        public StockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Deliberately bare [Authorize] — this is the exact action POS's
        // own WarehouseCatalogClient calls service-to-service
        // (GetAvailableQuantityAsync, C2) using a token that carries no
        // Role claim at all (see ServiceAuthHandler). Adding a Roles
        // requirement here would 403 every checkout in the system.
        [HttpGet("{itemId:int}")]
        [ProducesResponseType(typeof(IEnumerable<StockLevelDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<StockLevelDto>>> GetByItem(int itemId)
        {
            return Ok(await _mediator.Send(new GetStockLevelsQuery { ItemId = itemId }));
        }

        [HttpPost("receive")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(StockLevelDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<StockLevelDto>> Receive([FromBody] ReceiveStockCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("adjust")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(StockLevelDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<StockLevelDto>> Adjust([FromBody] AdjustStockCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("transfer")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(TransferStockResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<TransferStockResultDto>> Transfer([FromBody] TransferStockCommand command)
        {
            return Ok(await _mediator.Send(command));
        }
    }
}
