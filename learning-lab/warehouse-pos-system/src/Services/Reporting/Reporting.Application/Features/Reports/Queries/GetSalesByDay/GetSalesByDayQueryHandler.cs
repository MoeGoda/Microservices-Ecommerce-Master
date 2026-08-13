using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetSalesByDay
{
    public class GetSalesByDayQueryHandler : IRequestHandler<GetSalesByDayQuery, IEnumerable<SalesByDayDto>>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;

        public GetSalesByDayQueryHandler(ISaleRecordRepository saleRecordRepository)
        {
            _saleRecordRepository = saleRecordRepository;
        }

        public Task<IEnumerable<SalesByDayDto>> Handle(GetSalesByDayQuery request, CancellationToken cancellationToken)
        {
            return _saleRecordRepository.GetSalesByDay();
        }
    }
}
