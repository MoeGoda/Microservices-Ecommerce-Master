using FluentValidation;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
    {
        public CreatePurchaseOrderCommandValidator()
        {
            RuleFor(c => c.SupplierId).GreaterThan(0);
            RuleFor(c => c.Notes).MaximumLength(1000);
            RuleFor(c => c.CreatedByUserId).GreaterThan(0);
            RuleFor(c => c.Lines).NotEmpty().WithMessage("A purchase order needs at least one line.");
            RuleForEach(c => c.Lines).SetValidator(new CreatePurchaseOrderLineRequestValidator());
        }
    }

    public class CreatePurchaseOrderLineRequestValidator : AbstractValidator<CreatePurchaseOrderLineRequest>
    {
        public CreatePurchaseOrderLineRequestValidator()
        {
            RuleFor(l => l.ItemId).GreaterThan(0);
            RuleFor(l => l.UnitOfMeasureId).GreaterThan(0);
            RuleFor(l => l.OrderedQuantity).GreaterThan(0);
            RuleFor(l => l.UnitCost).GreaterThanOrEqualTo(0);
        }
    }
}
