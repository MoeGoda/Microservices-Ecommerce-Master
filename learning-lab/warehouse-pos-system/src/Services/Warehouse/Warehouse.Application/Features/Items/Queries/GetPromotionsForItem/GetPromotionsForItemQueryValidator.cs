using FluentValidation;

namespace Warehouse.Application.Features.Items.Queries.GetPromotionsForItem
{
    public class GetPromotionsForItemQueryValidator : AbstractValidator<GetPromotionsForItemQuery>
    {
        public GetPromotionsForItemQueryValidator()
        {
            RuleFor(q => q.ItemId).GreaterThan(0);
        }
    }
}
