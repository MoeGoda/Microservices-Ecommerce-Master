using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Items.Commands.AddItemBarcode
{
    public class AddItemBarcodeCommandHandler : IRequestHandler<AddItemBarcodeCommand, ItemBarcodeDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IItemBarcodeRepository _itemBarcodeRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AddItemBarcodeCommandHandler(
            IItemRepository itemRepository,
            IItemBarcodeRepository itemBarcodeRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _itemBarcodeRepository = itemBarcodeRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ItemBarcodeDto> Handle(AddItemBarcodeCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            if (await _itemBarcodeRepository.BarcodeExists(request.Barcode))
            {
                throw new ConflictException($"Barcode '{request.Barcode}' is already assigned to another item.");
            }

            if (request.IsPrimary)
            {
                var currentPrimary = await _itemBarcodeRepository.GetPrimary(item.Id);
                if (currentPrimary is not null)
                {
                    currentPrimary.IsPrimary = false;
                    await _itemBarcodeRepository.UpdateAsync(currentPrimary);
                }
            }

            var barcode = new ItemBarcode
            {
                ItemId = item.Id,
                Barcode = request.Barcode,
                BarcodeType = request.BarcodeType,
                IsPrimary = request.IsPrimary,
            };
            await _itemBarcodeRepository.AddAsync(barcode);

            // The demotion of the old primary (if any) and the insert of
            // the new one commit together — the filtered unique index on
            // (ItemId WHERE IsPrimary = 1) is never violated, even
            // momentarily, because both changes land in one SaveChanges call.
            await _unitOfWork.SaveChangesAsync();

            return ItemBarcodeDto.FromEntity(barcode);
        }
    }
}
