using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Reports.Queries.GetPurchaseOrderAging
{
    public class GetPurchaseOrderAgingQueryHandler : IRequestHandler<GetPurchaseOrderAgingQuery, IEnumerable<PurchaseOrderAgingLineDto>>
    {
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;

        public GetPurchaseOrderAgingQueryHandler(IPurchaseOrderRepository purchaseOrderRepository)
        {
            _purchaseOrderRepository = purchaseOrderRepository;
        }

        public async Task<IEnumerable<PurchaseOrderAgingLineDto>> Handle(GetPurchaseOrderAgingQuery request, CancellationToken cancellationToken)
        {
            var orders = await _purchaseOrderRepository.GetAllForAgingReport();
            var now = DateTime.UtcNow;

            return orders.Select(order => new PurchaseOrderAgingLineDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                SupplierName = order.Supplier.Name,
                Status = order.Status.ToString(),
                OrderedAtUtc = order.OrderedAtUtc,
                // Only meaningful once an order has actually been
                // submitted — see the DTO's own comment.
                AgeDaysSinceOrdered = order.Status is PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived
                    ? (int)(now - order.OrderedAtUtc!.Value).TotalDays
                    : null,
                TotalCost = order.Lines.Sum(l => l.OrderedQuantity * l.UnitCost),
                ReceivedValue = order.Lines.Sum(l => l.ReceivedQuantity * l.UnitCost),
            });
        }
    }
}
