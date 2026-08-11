using FluentValidation;

namespace Warehouse.Application.Features.Items.Commands.CreateItem
{
    // Shape-only checks. Whether the Sku/Barcode is already taken and
    // whether CategoryId/BaseUnitOfMeasureId/ParentItemId actually exist
    // are business/data checks that need a database round trip — those
    // stay in the handler, same as RegisterCommandHandler's UserNameExists
    // check in Identity (A1).
    public class CreateItemCommandValidator : AbstractValidator<CreateItemCommand>
    {
        public CreateItemCommandValidator()
        {
            RuleFor(c => c.Sku).NotEmpty().MaximumLength(50);
            RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
            RuleFor(c => c.Description).MaximumLength(1000);
            RuleFor(c => c.UnitPrice).GreaterThanOrEqualTo(0);
            RuleFor(c => c.CategoryId).GreaterThan(0);
            RuleFor(c => c.BaseUnitOfMeasureId).GreaterThan(0);
            RuleFor(c => c.ParentItemId).GreaterThan(0).When(c => c.ParentItemId.HasValue);
            RuleFor(c => c.Barcode).NotEmpty().MaximumLength(50);
            RuleFor(c => c.BarcodeType).IsInEnum();
        }
    }
}
