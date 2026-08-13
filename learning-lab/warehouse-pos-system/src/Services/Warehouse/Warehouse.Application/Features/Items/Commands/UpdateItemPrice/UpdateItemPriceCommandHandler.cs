using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.UpdateItemPrice
{
    public class UpdateItemPriceCommandHandler : IRequestHandler<UpdateItemPriceCommand, ItemDetailDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemPriceHistoryRepository _itemPriceHistoryRepository;
        private readonly IItemBarcodeRepository _itemBarcodeRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateItemPriceCommandHandler(
            IItemRepository itemRepository,
            IItemPriceHistoryRepository itemPriceHistoryRepository,
            IItemBarcodeRepository itemBarcodeRepository,
            IItemUnitRepository itemUnitRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _itemPriceHistoryRepository = itemPriceHistoryRepository;
            _itemBarcodeRepository = itemBarcodeRepository;
            _itemUnitRepository = itemUnitRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ItemDetailDto> Handle(UpdateItemPriceCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            // Re-submitting the same price isn't a "change" — recording a
            // history row for it would just be noise a real audit trail
            // has to scroll past to find the changes that actually happened.
            if (item.UnitPrice != request.NewPrice)
            {
                await _itemPriceHistoryRepository.AddAsync(new ItemPriceHistory
                {
                    ItemId = item.Id,
                    OldPrice = item.UnitPrice,
                    NewPrice = request.NewPrice,
                });

                item.UnitPrice = request.NewPrice;
                await _itemRepository.UpdateAsync(item);
                await _unitOfWork.SaveChangesAsync();
            }

            var barcodes = await _itemBarcodeRepository.GetByItem(item.Id);
            var units = await _itemUnitRepository.GetByItem(item.Id);
            var variants = await _itemRepository.GetVariants(item.Id);

            return ItemDetailDto.FromEntity(item, barcodes, units, variants);
        }
    }
}
