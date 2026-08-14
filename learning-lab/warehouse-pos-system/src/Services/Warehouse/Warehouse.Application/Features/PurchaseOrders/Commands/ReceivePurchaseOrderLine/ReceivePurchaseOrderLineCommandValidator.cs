using FluentValidation;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrderLine
{
    public class ReceivePurchaseOrderLineCommandValidator : AbstractValidator<ReceivePurchaseOrderLineCommand>
    {
        public ReceivePurchaseOrderLineCommandValidator()
        {
            RuleFor(c => c.PurchaseOrderId).GreaterThan(0);
            RuleFor(c => c.PurchaseOrderLineId).GreaterThan(0);
            RuleFor(c => c.LocationId).GreaterThan(0);
            RuleFor(c => c.Quantity).GreaterThan(0);
            RuleFor(c => c.Reference).MaximumLength(100);
        }
    }
}
