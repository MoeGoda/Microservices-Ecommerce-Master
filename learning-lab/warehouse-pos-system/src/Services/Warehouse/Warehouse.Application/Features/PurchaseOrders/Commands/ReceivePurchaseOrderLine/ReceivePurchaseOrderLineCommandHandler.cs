using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Stock;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.PurchaseOrders.Commands.ReceivePurchaseOrderLine
{
    public class ReceivePurchaseOrderLineCommandHandler : IRequestHandler<ReceivePurchaseOrderLineCommand, PurchaseOrderDetailDto>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly IItemUnitRepository _itemUnitRepository;
        private readonly StockAdjustmentStager _stager;
        private readonly IUnitOfWork _unitOfWork;

        public ReceivePurchaseOrderLineCommandHandler(
            IPurchaseOrderRepository purchaseOrderRepository,
            IItemUnitRepository itemUnitRepository,
            StockAdjustmentStager stager,
            IUnitOfWork unitOfWork)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
            _itemUnitRepository = itemUnitRepository;
            _stager = stager;
            _unitOfWork = unitOfWork;
        }

        public async Task<PurchaseOrderDetailDto> Handle(ReceivePurchaseOrderLineCommand request, CancellationToken cancellationToken)
        {
            var order = await _purchaseOrderRepository.GetById(request.PurchaseOrderId)
                ?? throw new NotFoundException(nameof(PurchaseOrder), request.PurchaseOrderId);

            if (order.Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
            {
                throw new ConflictException($"Purchase order '{order.OrderNumber}' is {order.Status} — only an Ordered or PartiallyReceived order can receive stock.");
            }

            var line = order.Lines.FirstOrDefault(l => l.Id == request.PurchaseOrderLineId)
                ?? throw new NotFoundException(nameof(PurchaseOrderLine), request.PurchaseOrderLineId);

            var remaining = line.OrderedQuantity - line.ReceivedQuantity;
            if (request.Quantity > remaining)
            {
                throw new ConflictException(
                    $"Only {remaining} '{line.UnitOfMeasure.Code}' of '{line.Item.Sku}' remain on this line — can't receive {request.Quantity}.");
            }

            var baseQuantity = await ConvertToBaseUnit(line, request.Quantity);

            // Same staging path ReceiveStockCommandHandler uses —
            // createIfMissing: true for the identical reason: a PO
            // receipt can be the first stock this item has ever had at
            // this location.
            await _stager.Stage(
                line.ItemId,
                request.LocationId,
                baseQuantity,
                StockTransactionReason.PurchaseOrderReceived,
                request.Reference ?? order.OrderNumber,
                createIfMissing: true);

            line.ReceivedQuantity += request.Quantity;

            // Derived purely from every line's own ReceivedQuantity vs.
            // OrderedQuantity — never set directly by a request. A line
            // that was never ordered any quantity at all can't exist
            // (CreatePurchaseOrderCommandValidator requires
            // OrderedQuantity > 0), so "every line fully received" is a
            // safe stand-in for "the whole order is complete."
            order.Status = order.Lines.All(l => l.ReceivedQuantity >= l.OrderedQuantity)
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.PartiallyReceived;

            await _purchaseOrderRepository.UpdateAsync(order);

            // Staged (Stage()) and direct (line/order) changes commit in
            // the SAME call — either the stock update, the line's
            // ReceivedQuantity, and the order's Status all land together,
            // or none of them do.
            await _unitOfWork.SaveChangesAsync();

            return PurchaseOrderDetailDto.FromEntity(order);
        }

        // Mirrors ReceiveStockCommandHandler's own ConvertToBaseUnit —
        // duplicated rather than extracted, so this new command carries
        // zero risk of changing that existing, already-verified handler.
        private async Task<int> ConvertToBaseUnit(PurchaseOrderLine line, decimal quantity)
        {
            if (line.UnitOfMeasureId == line.Item.BaseUnitOfMeasureId)
            {
                return (int)quantity;
            }

            var itemUnit = await _itemUnitRepository.GetByItemAndUnit(line.ItemId, line.UnitOfMeasureId)
                ?? throw new NotFoundException(nameof(ItemUnit), $"item {line.ItemId}, unit {line.UnitOfMeasureId}");

            var rawBaseQuantity = quantity * itemUnit.ConversionFactor;
            if (rawBaseQuantity != Math.Floor(rawBaseQuantity))
            {
                throw new ConflictException(
                    $"Converting {quantity} '{line.UnitOfMeasure.Code}' of '{line.Item.Sku}' to its base unit " +
                    $"({line.Item.BaseUnitOfMeasure.Code}) yields {rawBaseQuantity}, not a whole number — check the conversion factor.");
            }

            return (int)rawBaseQuantity;
        }
    }
}
