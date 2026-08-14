using FluentValidation;

namespace Warehouse.Application.Features.PurchaseOrders.Queries.GetPurchaseOrders
{
    public class GetPurchaseOrdersQueryValidator : AbstractValidator<GetPurchaseOrdersQuery>
    {
        public GetPurchaseOrdersQueryValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        }
    }
}
