using System.Net;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Features.Reports.Queries.GetSales;
using Reporting.Application.Features.Reports.Queries.GetStockLevels;
using Reporting.Application.Models;

namespace Reporting.API.Controllers
{
    // Deliberately named ReadModels, not Reports — these are raw dumps of
    // what's been ingested so far (proving the read models this step
    // built are actually correct and queryable), not the aggregated
    // sales-by-day/top-selling/low-stock reports D2 will build on top of
    // them. A browser-facing caller (D2's Angular dashboards) routes
    // through the gateway's /Reporting/... upstream; unlike
    // EventsController, that's the whole reason this one IS in
    // ocelot.json.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class ReadModelsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReadModelsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("sales")]
        [ProducesResponseType(typeof(IEnumerable<SaleRecordDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<SaleRecordDto>>> GetSales()
        {
            return Ok(await _mediator.Send(new GetSalesQuery()));
        }

        [HttpGet("stock-levels")]
        [ProducesResponseType(typeof(IEnumerable<StockLevelRecordDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<StockLevelRecordDto>>> GetStockLevels()
        {
            return Ok(await _mediator.Send(new GetStockLevelsQuery()));
        }
    }
}
