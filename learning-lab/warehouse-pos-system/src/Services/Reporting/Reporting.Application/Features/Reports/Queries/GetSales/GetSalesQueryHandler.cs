using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSales
{
    public class GetSalesQueryHandler : IRequestHandler<GetSalesQuery, IEnumerable<SaleRecordDto>>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;

        public GetSalesQueryHandler(ISaleRecordRepository saleRecordRepository)
        {
            _saleRecordRepository = saleRecordRepository;
        }

        public async Task<IEnumerable<SaleRecordDto>> Handle(GetSalesQuery request, CancellationToken cancellationToken)
        {
            var records = await _saleRecordRepository.GetAll();
            return records.Select(SaleRecordDto.FromEntity);
        }
    }
}
