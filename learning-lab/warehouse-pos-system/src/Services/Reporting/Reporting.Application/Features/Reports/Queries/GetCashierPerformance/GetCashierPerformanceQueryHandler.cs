using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetCashierPerformance
{
    public class GetCashierPerformanceQueryHandler : IRequestHandler<GetCashierPerformanceQuery, IEnumerable<CashierPerformanceDto>>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;

        public GetCashierPerformanceQueryHandler(ISaleRecordRepository saleRecordRepository)
        {
            _saleRecordRepository = saleRecordRepository;
        }

        public Task<IEnumerable<CashierPerformanceDto>> Handle(GetCashierPerformanceQuery request, CancellationToken cancellationToken)
        {
            return _saleRecordRepository.GetCashierPerformance(request.FromUtc, request.ToUtc);
        }
    }
}
