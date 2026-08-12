using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.AddItemUnit
{
    public class AddItemUnitCommandHandler : IRequestHandler<AddItemUnitCommand, ItemUnitDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddItemUnitCommandHandler(
            IItemRepository itemRepository,
            IItemUnitRepository itemUnitRepository,
            IUnitOfMeasureRepository unitOfMeasureRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _itemUnitRepository = itemUnitRepository;
            _unitOfMeasureRepository = unitOfMeasureRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ItemUnitDto> Handle(AddItemUnitCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var unitOfMeasure = await _unitOfMeasureRepository.GetById(request.UnitOfMeasureId)
                ?? throw new NotFoundException(nameof(UnitOfMeasure), request.UnitOfMeasureId);

            if (unitOfMeasure.Id == item.BaseUnitOfMeasureId)
            {
                throw new ConflictException($"'{unitOfMeasure.Code}' is already this item's base unit; base units aren't stored as a conversion.");
            }

            if (await _itemUnitRepository.GetByItemAndUnit(item.Id, unitOfMeasure.Id) is not null)
            {
                throw new ConflictException($"Item '{item.Sku}' already has a conversion defined for '{unitOfMeasure.Code}'.");
            }

            var itemUnit = new ItemUnit
            {
                ItemId = item.Id,
                UnitOfMeasureId = unitOfMeasure.Id,
                UnitOfMeasure = unitOfMeasure,
                ConversionFactor = request.ConversionFactor,
            };
            await _itemUnitRepository.AddAsync(itemUnit);
            await _unitOfWork.SaveChangesAsync();

            return ItemUnitDto.FromEntity(itemUnit);
        }
    }
}
