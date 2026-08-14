using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Reports.Queries.GetPurchaseOrderAging
{
    public class GetPurchaseOrderAgingQuery : IRequest<IEnumerable<PurchaseOrderAgingLineDto>>
    {
    }
}
