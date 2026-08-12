using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Stock.Queries.GetStockLevels
{
    // Every location's balance for one item — e.g. "Store A: 250, Store B:
    // 120, Warehouse: 1,500" for a single product.
    public class GetStockLevelsQuery : IRequest<IEnumerable<StockLevelDto>>
    {
        public int ItemId { get; set; }
    }
}
