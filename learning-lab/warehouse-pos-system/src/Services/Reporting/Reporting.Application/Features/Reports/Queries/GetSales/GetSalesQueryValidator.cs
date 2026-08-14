using FluentValidation;

namespace Reporting.Application.Features.Reports.Queries.GetSales
{
    public class GetSalesQueryValidator : AbstractValidator<GetSalesQuery>
    {
        public GetSalesQueryValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        }
    }
}
