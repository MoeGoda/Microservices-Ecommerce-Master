using Common.Pagination;
using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetStockMovements
{
    public class GetStockMovementsQueryHandler : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementRecordDto>>
    {
        private readonly IStockMovementRecordRepository _stockMovementRecordRepository;

        public GetStockMovementsQueryHandler(IStockMovementRecordRepository stockMovementRecordRepository)
        {
            _stockMovementRecordRepository = stockMovementRecordRepository;
        }

        public async Task<PagedResult<StockMovementRecordDto>> Handle(GetStockMovementsQuery request, CancellationToken cancellationToken)
        {
            var (records, totalCount) = await _stockMovementRecordRepository.GetPaged(
                request.Page, request.PageSize, request.FromUtc, request.ToUtc, request.ItemId, request.LocationId);

            var dtos = records.Select(StockMovementRecordDto.FromEntity).ToList();
            return PagedResult<StockMovementRecordDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}
