using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetLowStock
{
    public class GetLowStockQueryHandler : IRequestHandler<GetLowStockQuery, IEnumerable<StockLevelRecordDto>>
    {
        private readonly IStockLevelRecordRepository _stockLevelRecordRepository;

        public GetLowStockQueryHandler(IStockLevelRecordRepository stockLevelRecordRepository)
        {
            _stockLevelRecordRepository = stockLevelRecordRepository;
        }

        public async Task<IEnumerable<StockLevelRecordDto>> Handle(GetLowStockQuery request, CancellationToken cancellationToken)
        {
            var records = await _stockLevelRecordRepository.GetLowStock();
            return records.Select(StockLevelRecordDto.FromEntity);
        }
    }
}
