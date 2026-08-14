using Common.Pagination;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSuppliers
{
    public class GetSuppliersQueryHandler : IRequestHandler<GetSuppliersQuery, PagedResult<SupplierDto>>
    {
        private readonly ISupplierRepository _supplierRepository;

        public GetSuppliersQueryHandler(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public async Task<PagedResult<SupplierDto>> Handle(GetSuppliersQuery request, CancellationToken cancellationToken)
        {
            var (suppliers, totalCount) = await _supplierRepository.GetPaged(request.Page, request.PageSize);
            var dtos = suppliers.Select(SupplierDto.FromEntity).ToList();
            return PagedResult<SupplierDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}
