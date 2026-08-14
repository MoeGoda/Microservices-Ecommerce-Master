using Common.Pagination;
using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSales
{
    public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, PagedResult<SaleRecordDto>>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;

        public GetSalesQueryHandler(ISaleRecordRepository saleRecordRepository)
        {
            _saleRecordRepository = saleRecordRepository;
        }

        public async Task<PagedResult<SaleRecordDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
        {
            var (records, totalCount) = await _saleRecordRepository.GetPaged(request.Page, request.PageSize);
            var dtos = records.Select(SaleRecordDto.FromEntity).ToList();
            return PagedResult<SaleRecordDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}
