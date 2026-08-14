using System.Net;
using Common.Pagination;
using Common.Security;
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
    // ocelot.json. Same F2 role restriction as ReportsController, same
    // reasoning — nothing calls into this controller service-to-service.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager}")]
    public class ReadModelsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReadModelsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("sales")]
        [ProducesResponseType(typeof(PagedResult<SaleRecordDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResult<SaleRecordDto>>> GetSales([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _mediator.Send(new GetSalesQuery { Page = page, PageSize = pageSize }));
        }

        [HttpGet("stock-levels")]
        [ProducesResponseType(typeof(IEnumerable<StockLevelRecordDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<StockLevelRecordDto>>> GetStockLevels()
        {
            return Ok(await _mediator.Send(new GetStockLevelsQuery()));
        }
    }
}
