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
        private readonly IMediator _mediator;

        public StockController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{itemId:int}")]
        [ProducesResponseType(typeof(IEnumerable<StockLevelDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<StockLevelDto>>> GetByItem(int itemId)
        {
            return Ok(await _mediator.Send(new GetStockLevelsQuery { ItemId = itemId }));
        }

        [HttpPost("receive")]
        [ProducesResponseType(typeof(StockLevelDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<StockLevelDto>> Receive([FromBody] ReceiveStockCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("adjust")]
        [ProducesResponseType(typeof(StockLevelDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<StockLevelDto>> Adjust([FromBody] AdjustStockCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransferStockResultDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<TransferStockResultDto>> Transfer([FromBody] TransferStockCommand command)
        {
            return Ok(await _mediator.Send(command));
        }
    }
}
