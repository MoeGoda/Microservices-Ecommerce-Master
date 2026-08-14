using Common.Pagination;
using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Warehouse.Application.Features.Suppliers.Commands.CreateSupplier;
using Warehouse.Application.Features.Suppliers.Commands.SetSupplierActive;
using Warehouse.Application.Features.Suppliers.Queries.GetSuppliers;
using Warehouse.Application.Models;

namespace Warehouse.API.Controllers
{
    // Same class-level bare [Authorize] + per-action Roles layering as
    // ItemsController — nothing here is ever reached service-to-service,
    // so there's no read action that needs to stay role-unrestricted the
    // way ResolveBarcode does.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private const string CatalogManagerRoles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.WarehouseStaff}";

        private readonly IMediator _mediator;

        public SuppliersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<SupplierDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResult<SupplierDto>>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _mediator.Send(new GetSuppliersQuery { Page = page, PageSize = pageSize }));
        }

        [HttpPost]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(SupplierDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateSupplierCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{id:int}/active")]
        [Authorize(Roles = CatalogManagerRoles)]
        [ProducesResponseType(typeof(SupplierDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<SupplierDto>> SetActive(int id, [FromBody] SetSupplierActiveRequest body)
        {
            return Ok(await _mediator.Send(new SetSupplierActiveCommand { SupplierId = id, IsActive = body.IsActive }));
        }
    }

    public class SetSupplierActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
