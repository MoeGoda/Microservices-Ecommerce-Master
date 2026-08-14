using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.Reports.Queries.GetInventoryValuation
{
    public class GetInventoryValuationQueryHandler : IRequestHandler<GetInventoryValuationQuery, IEnumerable<InventoryValuationLineDto>>
    {
        private readonly IStockLevelRepository _stockLevelRepository;

        public GetInventoryValuationQueryHandler(IStockLevelRepository stockLevelRepository)
        {
            _stockLevelRepository = stockLevelRepository;
        }

        public Task<IEnumerable<InventoryValuationLineDto>> Handle(GetInventoryValuationQuery request, CancellationToken cancellationToken)
        {
            return _stockLevelRepository.GetInventoryValuation();
        }
    }
}
