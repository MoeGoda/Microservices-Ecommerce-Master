using Common.Pagination;
using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSuppliers
{
    public class GetSuppliersQuery : IRequest<PagedResult<SupplierDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
