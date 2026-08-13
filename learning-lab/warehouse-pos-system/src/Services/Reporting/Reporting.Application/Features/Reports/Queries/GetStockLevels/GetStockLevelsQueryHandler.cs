using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetStockLevels
{
    public class GetStockLevelsQueryHandler : IRequestHandler<GetStockLevelsQuery, IEnumerable<StockLevelRecordDto>>
    {
        private readonly IStockLevelRecordRepository _stockLevelRecordRepository;

        public GetStockLevelsQueryHandler(IStockLevelRecordRepository stockLevelRecordRepository)
        {
            _stockLevelRecordRepository = stockLevelRecordRepository;
        }

        public async Task<IEnumerable<StockLevelRecordDto>> Handle(GetStockLevelsQuery request, CancellationToken cancellationToken)
        {
            var records = await _stockLevelRecordRepository.GetAll();
            return records.Select(StockLevelRecordDto.FromEntity);
        }
    }
}
