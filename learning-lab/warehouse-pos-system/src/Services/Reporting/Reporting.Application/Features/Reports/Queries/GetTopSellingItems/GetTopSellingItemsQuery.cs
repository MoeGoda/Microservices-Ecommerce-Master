using MediatR;
using Reporting.Application.Models;

namespace Reporting.Application.Features.Reports.Queries.GetTopSellingItems
{
    public class GetTopSellingItemsQuery : IRequest<IEnumerable<TopSellingItemDto>>
    {
        public int Take { get; set; } = 10;
    }
}
