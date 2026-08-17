using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.CreatePurchaseOrder
{
    public class CreatePurchaseOrderCommandHandler : IRequestHandler<CreatePurchaseOrderCommand, PurchaseOrderDetailDto>
    {
        private readonly ISupplierRepository _supplierRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreatePurchaseOrderCommandHandler(
            ISupplierRepository supplierRepository,
            IItemRepository itemRepository,
            IUnitOfMeasureRepository unitOfMeasureRepository,
            IItemUnitRepository itemUnitRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            IUnitOfWork unitOfWork)
        {
            _supplierRepository = supplierRepository;
            _itemRepository = itemRepository;
            _unitOfMeasureRepository = unitOfMeasureRepository;
            _itemUnitRepository = itemUnitRepository;
            _purchaseOrderRepository = purchaseOrderRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseOrderDetailDto> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            var supplier = await _supplierRepository.GetById(request.SupplierId)
                ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

            if (!supplier.IsActive)
            {
                throw new ConflictException($"Supplier '{supplier.Name}' is deactivated — reactivate it before placing a new order.");
            }

            var order = new PurchaseOrder
            {
                // A placeholder — overwritten below once SaveChangesAsync
                // assigns this row's real Id. The unique index on
                // OrderNumber means two orders can never collide on this
                // in the meantime; nothing else reads it before that
                // second save.
                OrderNumber = string.Empty,
                SupplierId = supplier.Id,
                Supplier = supplier,
                Status = PurchaseOrderStatus.Draft,
                Notes = request.Notes,
                CreatedByUserId = request.CreatedByUserId,
            };

            foreach (var lineRequest in request.Lines)
            {
                var item = await _itemRepository.GetById(lineRequest.ItemId)
                    ?? throw new NotFoundException(nameof(Item), lineRequest.ItemId);

                var unitOfMeasure = await _unitOfMeasureRepository.GetById(lineRequest.UnitOfMeasureId)
                    ?? throw new NotFoundException(nameof(UnitOfMeasure), lineRequest.UnitOfMeasureId);

                // Fail here, not weeks later at receiving time.
                // ReceivePurchaseOrderLineCommandHandler.ConvertToBaseUnit
                // needs either the item's own base unit or an existing
                // ItemUnit conversion to turn a received quantity into a
                // stock movement — a line ordered in a unit the item has
                // no conversion for would create a PO that can never
                // actually be received.
                if (unitOfMeasure.Id != item.BaseUnitOfMeasureId
                    && await _itemUnitRepository.GetByItemAndUnit(item.Id, unitOfMeasure.Id) is null)
                {
                    throw new ConflictException(
                        $"'{item.Sku}' has no unit conversion set up for '{unitOfMeasure.Code}' — add that unit to the item first, or order it in its base unit ('{item.BaseUnitOfMeasure.Code}').");
                }

                order.Lines.Add(new PurchaseOrderLine
                {
                    Item = item,
                    ItemId = item.Id,
                    UnitOfMeasure = unitOfMeasure,
                    UnitOfMeasureId = unitOfMeasure.Id,
                    OrderedQuantity = lineRequest.OrderedQuantity,
                    ReceivedQuantity = 0,
                    UnitCost = lineRequest.UnitCost,
                });
            }

            await _purchaseOrderRepository.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            order.OrderNumber = $"PO-{order.Id:D6}";
            await _purchaseOrderRepository.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return PurchaseOrderDetailDto.FromEntity(order);
        }
    }
}
