using Common.Pagination;
using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Customers.Queries.SearchCustomers
{
    // Name/phone substring search — the register's customer-lookup box.
    public class SearchCustomersQuery : IRequest<PagedResult<CustomerDto>>
    {
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
