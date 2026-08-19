using MediatR;
using POS.Application.Models;

namespace POS.Application.Features.Sales.Queries.GetInProgressSales
{
    // The "held sales" list — every InProgress sale, optionally narrowed
    // to one register's LocationId, so a cashier can put a basket aside
    // and pick a different (or the same) one back up.
    public class GetInProgressSalesQuery : IRequest<IEnumerable<SaleDto>>
    {
        public int? LocationId { get; set; }
    }
}
