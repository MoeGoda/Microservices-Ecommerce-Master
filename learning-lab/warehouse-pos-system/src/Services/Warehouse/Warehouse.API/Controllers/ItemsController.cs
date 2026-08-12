using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Warehouse.Application.Features.Items.Commands.AddItemBarcode;
using Warehouse.Application.Features.Items.Commands.AddItemUnit;
using Warehouse.Application.Features.Items.Commands.CreateItem;
using Warehouse.Application.Features.Items.Commands.CreatePromotion;
using Warehouse.Application.Features.Items.Commands.UpdateItemPrice;
using Warehouse.Application.Features.Items.Queries.GetAllItems;
using Warehouse.Application.Features.Items.Queries.GetItemById;
using Warehouse.Application.Features.Items.Queries.GetItemPriceHistory;
using Warehouse.Application.Features.Items.Queries.GetItemVariants;
using Warehouse.Application.Features.Items.Queries.ResolveBarcode;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    // Unlike Identity.API's /register and /login, there is no anonymous
    // route anywhere in Warehouse — every action here needs a caller who
    // already has a token, so [Authorize] sits once at the controller
    // level rather than repeated on every action.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ItemsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ItemsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ItemSummaryDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<ItemSummaryDto>>> GetAll()
        {
            return Ok(await _mediator.Send(new GetAllItemsQuery()));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ItemDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ItemDetailDto>> GetById(int id)
        {
            return Ok(await _mediator.Send(new GetItemByIdQuery { Id = id }));
        }

        [HttpGet("{id:int}/variants")]
        [ProducesResponseType(typeof(IEnumerable<ItemSummaryDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<ItemSummaryDto>>> GetVariants(int id)
        {
            return Ok(await _mediator.Send(new GetItemVariantsQuery { ParentItemId = id }));
        }

        // "barcodes" as a literal segment here, distinct from the
        // int-constrained {id} above — GetById never matches a request for
        // "/Items/barcodes/...".
        [HttpGet("barcodes/{barcode}")]
        [ProducesResponseType(typeof(ItemDetailDto), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<ActionResult<ItemDetailDto>> ResolveBarcode(string barcode)
        {
            // ResolveBarcodeQuery itself returns null rather than throwing
            // NotFoundException — an unknown scan is an ordinary outcome at
            // the Application layer, not an exceptional one (see B2). This
            // is the seam where that ordinary null becomes an HTTP 404,
            // which IS the correct status for "this resource doesn't exist" —
            // translating it here doesn't change B2's own reasoning for not
            // throwing internally.
            var result = await _mediator.Send(new ResolveBarcodeQuery { Barcode = barcode });
            return result is null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ItemDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ItemDetailDto>> Create([FromBody] CreateItemCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        // ItemId is already in the URL — overwriting whatever the body
        // claims makes the URL authoritative and rules out a
        // route-says-item-5-but-body-says-item-9 mismatch entirely, rather
        // than validating for it.
        [HttpPost("{id:int}/barcodes")]
        [ProducesResponseType(typeof(ItemBarcodeDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ItemBarcodeDto>> AddBarcode(int id, [FromBody] AddItemBarcodeCommand command)
        {
            command.ItemId = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{id:int}/units")]
        [ProducesResponseType(typeof(ItemUnitDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ItemUnitDto>> AddUnit(int id, [FromBody] AddItemUnitCommand command)
        {
            command.ItemId = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpPut("{id:int}/price")]
        [ProducesResponseType(typeof(ItemDetailDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<ItemDetailDto>> UpdatePrice(int id, [FromBody] UpdateItemPriceCommand command)
        {
            command.ItemId = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpGet("{id:int}/price-history")]
        [ProducesResponseType(typeof(IEnumerable<ItemPriceHistoryDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<ItemPriceHistoryDto>>> GetPriceHistory(int id)
        {
            return Ok(await _mediator.Send(new GetItemPriceHistoryQuery { ItemId = id }));
        }

        [HttpPost("{id:int}/promotions")]
        [ProducesResponseType(typeof(PromotionDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PromotionDto>> CreatePromotion(int id, [FromBody] CreatePromotionCommand command)
        {
            command.ItemId = id;
            return Ok(await _mediator.Send(command));
        }
    }
}
