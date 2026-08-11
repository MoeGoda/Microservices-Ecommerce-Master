using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.CreateItem
{
    public class CreateItemCommandHandler : IRequestHandler<CreateItemCommand, ItemDetailDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemBarcodeRepository _itemBarcodeRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateItemCommandHandler(
            IItemRepository itemRepository,
            IItemBarcodeRepository itemBarcodeRepository,
            ICategoryRepository categoryRepository,
            IUnitOfMeasureRepository unitOfMeasureRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _itemBarcodeRepository = itemBarcodeRepository;
            _categoryRepository = categoryRepository;
            _unitOfMeasureRepository = unitOfMeasureRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ItemDetailDto> Handle(CreateItemCommand request, CancellationToken cancellationToken)
        {
            if (await _itemRepository.SkuExists(request.Sku))
            {
                throw new ConflictException($"Sku '{request.Sku}' is already in use.");
            }

            if (await _itemBarcodeRepository.BarcodeExists(request.Barcode))
            {
                throw new ConflictException($"Barcode '{request.Barcode}' is already assigned to another item.");
            }

            var category = await _categoryRepository.GetById(request.CategoryId)
                ?? throw new NotFoundException(nameof(Category), request.CategoryId);

            var unitOfMeasure = await _unitOfMeasureRepository.GetById(request.BaseUnitOfMeasureId)
                ?? throw new NotFoundException(nameof(UnitOfMeasure), request.BaseUnitOfMeasureId);

            Item? parentItem = null;
            if (request.ParentItemId.HasValue)
            {
                parentItem = await _itemRepository.GetById(request.ParentItemId.Value)
                    ?? throw new NotFoundException(nameof(Item), request.ParentItemId.Value);
            }

            var item = new Item
            {
                Sku = request.Sku,
                Name = request.Name,
                Description = request.Description,
                UnitPrice = request.UnitPrice,
                CategoryId = category.Id,
                Category = category,
                BaseUnitOfMeasureId = unitOfMeasure.Id,
                BaseUnitOfMeasure = unitOfMeasure,
                ParentItemId = parentItem?.Id,
            };
            await _itemRepository.AddAsync(item);

            // Linked via the Item navigation, not item.Id directly — Id is
            // database-generated and doesn't exist yet (nothing has been
            // saved). EF Core's change tracker fixes up ItemBarcode.ItemId
            // once SaveChanges resolves the new Item's real key, as long as
            // both are tracked by the same context and linked this way.
            var barcode = new ItemBarcode
            {
                Item = item,
                Barcode = request.Barcode,
                BarcodeType = request.BarcodeType,
                IsPrimary = true,
            };
            await _itemBarcodeRepository.AddAsync(barcode);

            // One transaction: the item and its first barcode are created
            // together or not at all.
            await _unitOfWork.SaveChangesAsync();

            return ItemDetailDto.FromEntity(item, new[] { barcode }, Array.Empty<ItemUnit>(), Array.Empty<Item>());
        }
    }
}
