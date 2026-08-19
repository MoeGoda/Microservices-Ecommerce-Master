using Common.Pagination;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;

namespace POS.Application.Features.Customers.Queries.SearchCustomers
{
    public class SearchCustomersQueryHandler : IRequestHandler<SearchCustomersQuery, PagedResult<CustomerDto>>
    {
        private readonly ICustomerRepository _customerRepository;

        public SearchCustomersQueryHandler(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task<PagedResult<CustomerDto>> Handle(SearchCustomersQuery request, CancellationToken cancellationToken)
        {
            var customers = await _customerRepository.Search(request.Search, request.Page, request.PageSize);
            var totalCount = await _customerRepository.CountSearch(request.Search);

            var dtos = customers.Select(CustomerDto.FromEntity).ToList();
            return PagedResult<CustomerDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}
