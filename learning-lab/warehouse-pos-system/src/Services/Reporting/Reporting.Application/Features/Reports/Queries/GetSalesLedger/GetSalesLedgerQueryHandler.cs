using Common.Pagination;
using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSalesLedger
{
    public class GetSalesLedgerQueryHandler : IRequestHandler<GetSalesLedgerQuery, PagedResult<SalesLedgerEntryDto>>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;

        public GetSalesLedgerQueryHandler(ISaleRecordRepository saleRecordRepository)
        {
            _saleRecordRepository = saleRecordRepository;
        }

        public async Task<PagedResult<SalesLedgerEntryDto>> Handle(GetSalesLedgerQuery request, CancellationToken cancellationToken)
        {
            var (records, totalCount) = await _saleRecordRepository.GetLedgerPaged(request.Page, request.PageSize, request.FromUtc, request.ToUtc);
            var dtos = records.Select(SalesLedgerEntryDto.FromEntity).ToList();
            return PagedResult<SalesLedgerEntryDto>.Create(dtos, request.Page, request.PageSize, totalCount);
        }
    }
}
