using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetTopSellingItems
{
    public class GetTopSellingItemsQueryHandler : IRequestHandler<GetTopSellingItemsQuery, IEnumerable<TopSellingItemDto>>
    {
        private readonly ISaleLineRecordRepository _saleLineRecordRepository;

        public GetTopSellingItemsQueryHandler(ISaleLineRecordRepository saleLineRecordRepository)
        {
            _saleLineRecordRepository = saleLineRecordRepository;
        }

        public Task<IEnumerable<TopSellingItemDto>> Handle(GetTopSellingItemsQuery request, CancellationToken cancellationToken)
        {
            return _saleLineRecordRepository.GetTopSellingItems(request.Take);
        }
    }
}
