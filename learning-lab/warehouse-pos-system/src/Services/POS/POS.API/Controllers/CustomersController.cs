using System.Net;
using Common.Pagination;
using Common.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using POS.Application.Features.Customers.Commands.AdjustCustomerBalance;
using POS.Application.Features.Customers.Commands.CreateCustomer;
using POS.Application.Features.Customers.Commands.UpdateCustomer;
using POS.Application.Features.Customers.Queries.GetCustomerById;
using POS.Application.Features.Customers.Queries.SearchCustomers;
using POS.Application.Models;

namespace POS.API.Controllers
{
    // Same Cashier/Manager/Admin set SalesController already uses —
    // customer lookup/creation happens from the register itself, not a
    // separate back-office role.
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.Manager},{RoleNames.Cashier}")]
    public class CustomersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CustomersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<CustomerDto>), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<PagedResult<CustomerDto>>> Search([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            return Ok(await _mediator.Send(new SearchCustomersQuery { Search = search, Page = page, PageSize = pageSize }));
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CustomerDto>> GetById(int id)
        {
            return Ok(await _mediator.Send(new GetCustomerByIdQuery { Id = id }));
        }

        [HttpPost]
        [ProducesResponseType(typeof(CustomerDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(CustomerDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CustomerDto>> Update(int id, [FromBody] UpdateCustomerCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpPost("{id:int}/balance-adjustments")]
        [ProducesResponseType(typeof(CustomerDto), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CustomerDto>> AdjustBalance(int id, [FromBody] AdjustCustomerBalanceCommand command)
        {
            command.CustomerId = id;
            return Ok(await _mediator.Send(command));
        }
    }
}
