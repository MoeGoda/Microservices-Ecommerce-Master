using FluentValidation;

namespace Warehouse.Application.Features.Suppliers.Queries.GetSuppliers
{
    public class GetSuppliersQueryValidator : AbstractValidator<GetSuppliersQuery>
    {
        public GetSuppliersQueryValidator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
        }
    }
}
