using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Application.Exceptions;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.Sales.Commands.AddSaleLine
{
    public class AddSaleLineCommandHandler : IRequestHandler<AddSaleLineCommand, SaleDto>
    {
        private readonly ISaleRepository _saleRepository;
        private readonly ISaleLineRepository _saleLineRepository;
        private readonly IWarehouseCatalogClient _warehouseCatalogClient;
        private readonly IUnitOfWork _unitOfWork;

        public AddSaleLineCommandHandler(
            ISaleRepository saleRepository,
            ISaleLineRepository saleLineRepository,
            IWarehouseCatalogClient warehouseCatalogClient,
            IUnitOfWork unitOfWork)
        {
            _saleRepository = saleRepository;
            _saleLineRepository = saleLineRepository;
            _warehouseCatalogClient = warehouseCatalogClient;
            _unitOfWork = unitOfWork;
        }

        public async Task<SaleDto> Handle(AddSaleLineCommand request, CancellationToken cancellationToken)
        {
            var sale = await _saleRepository.GetById(request.SaleId)
                ?? throw new NotFoundException(nameof(Sale), request.SaleId);

            if (sale.Status != SaleStatus.InProgress)
            {
                throw new ConflictException($"Sale {sale.Id} is {sale.Status}; lines can only be added to a sale that is still InProgress.");
            }

            // The barcode validation half of this step: an unknown scan is
            // "not found," not a server error — a cashier scanning
            // something outside the catalog is an everyday occurrence.
            var item = await _warehouseCatalogClient.ResolveBarcodeAsync(request.Barcode, cancellationToken)
                ?? throw new NotFoundException("Item", request.Barcode);

            // The stock check half: against the SALE's own LocationId —
            // this register's location — not something the caller supplies
            // separately, since Sale already carries it (StartSaleCommand,
            // C1) and a mismatched second LocationId would just be a way
            // for the two to silently disagree.
            var availableQuantity = await _warehouseCatalogClient.GetAvailableQuantityAsync(item.ItemId, sale.LocationId, cancellationToken);
            if (availableQuantity < request.Quantity)
            {
                throw new InsufficientStockException(item.Sku, request.Quantity, availableQuantity);
            }

            var lineTotal = item.UnitPrice * request.Quantity;

            var line = new SaleLine
            {
                SaleId = sale.Id,
                Sale = sale,
                ItemId = item.ItemId,
                Sku = item.Sku,
                ItemName = item.ItemName,
                UnitPrice = item.UnitPrice,
                Quantity = request.Quantity,
                LineTotal = lineTotal,
            };
            await _saleLineRepository.AddAsync(line);

            sale.Total += lineTotal;
            await _saleRepository.UpdateAsync(sale);

            await _unitOfWork.SaveChangesAsync();

            var lines = await _saleLineRepository.GetBySale(sale.Id);
            return SaleDto.FromEntity(sale, lines);
        }
    }
}
