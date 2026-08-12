using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Warehouse.Application.Features.MasterData.Queries.GetCategories;
using Warehouse.Application.Features.MasterData.Queries.GetLocations;
using Warehouse.Application.Features.MasterData.Queries.GetUnitsOfMeasure;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    // One controller for three read-only lookups, mirroring
    // Warehouse.Application's own Features/MasterData grouping — these
    // exist purely to populate dropdowns on the Admin Panel's item form,
    // not because Category/Location/UnitOfMeasure need their own
    // full CRUD surface (they don't; see B1's README section on why
    // they're fixed, migration-seeded reference data).
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class MasterDataController : ControllerBase
    {
        private readonly IMediator _mediator;

        public MasterDataController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            return Ok(await _mediator.Send(new GetCategoriesQuery()));
        }

        [HttpGet("locations")]
        [ProducesResponseType(typeof(IEnumerable<LocationDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations()
        {
            return Ok(await _mediator.Send(new GetLocationsQuery()));
        }

        [HttpGet("units-of-measure")]
        [ProducesResponseType(typeof(IEnumerable<UnitOfMeasureDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<IEnumerable<UnitOfMeasureDto>>> GetUnitsOfMeasure()
        {
            return Ok(await _mediator.Send(new GetUnitsOfMeasureQuery()));
        }
    }
}
