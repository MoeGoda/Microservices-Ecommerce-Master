using FluentValidation;

namespace Reporting.Application.Features.Reports.Queries.GetTopSellingItems
{
    public class GetTopSellingItemsQueryValidator : AbstractValidator<GetTopSellingItemsQuery>
    {
        public GetTopSellingItemsQueryValidator()
        {
            RuleFor(q => q.Take).InclusiveBetween(1, 100);
        }
    }
}
