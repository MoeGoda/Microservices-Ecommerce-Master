using Common.Exceptions;
using Microsoft.Extensions.Options;
using Moq;
using POS.Application.Common;
using POS.Application.Contracts.Infrastructure;
using POS.Application.Contracts.Persistence;
using POS.Application.Exceptions;
using POS.Application.Features.Sales.Commands.AddSaleLine;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Sales.Commands
{
    public class AddSaleLineCommandHandlerTests
    {
        private readonly Mock<ISaleRepository> _saleRepository = new();
        private readonly Mock<ISaleLineRepository> _saleLineRepository = new();
        private readonly Mock<IWarehouseCatalogClient> _warehouseCatalogClient = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly IOptions<TaxSettings> _taxSettings = Options.Create(new TaxSettings { RatePercent = 10m });

        private AddSaleLineCommandHandler CreateHandler() => new(
            _saleRepository.Object,
            _saleLineRepository.Object,
            _warehouseCatalogClient.Object,
            _unitOfWork.Object,
            _taxSettings);

        private static Sale InProgressSale(int id = 1, int locationId = 1) => new()
        {
            Id = id,
            LocationId = locationId,
            Status = SaleStatus.InProgress,
        };

        [Fact]
        public async Task Handle_SaleNotFound_ThrowsNotFoundException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync((Sale?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new AddSaleLineCommand { SaleId = 1, Barcode = "123", Quantity = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_SaleNotInProgress_ThrowsConflictException()
        {
            var sale = InProgressSale();
            sale.Status = SaleStatus.Completed;
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new AddSaleLineCommand { SaleId = 1, Barcode = "123", Quantity = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_UnknownBarcode_ThrowsNotFoundException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(InProgressSale());
            _warehouseCatalogClient
                .Setup(c => c.ResolveBarcodeAsync("unknown", It.IsAny<CancellationToken>()))
                .ReturnsAsync((WarehouseItemLookup?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(new AddSaleLineCommand { SaleId = 1, Barcode = "unknown", Quantity = 1 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_RequestedQuantityExceedsAvailableStock_ThrowsInsufficientStockException()
        {
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(InProgressSale());
            _warehouseCatalogClient
                .Setup(c => c.ResolveBarcodeAsync("123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WarehouseItemLookup { ItemId = 1, Sku = "SKU-1", ItemName = "Widget", UnitPrice = 10m });
            _warehouseCatalogClient
                .Setup(c => c.GetAvailableQuantityAsync(1, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<InsufficientStockException>(() =>
                handler.Handle(new AddSaleLineCommand { SaleId = 1, Barcode = "123", Quantity = 5 }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_ItemHasActivePromotion_ManualDiscountIsDroppedNotStacked()
        {
            var sale = InProgressSale();
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _warehouseCatalogClient
                .Setup(c => c.ResolveBarcodeAsync("123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WarehouseItemLookup
                {
                    ItemId = 1,
                    Sku = "SKU-1",
                    ItemName = "Widget",
                    UnitPrice = 8m,
                    OriginalUnitPrice = 10m,
                    PromotionId = 42,
                });
            _warehouseCatalogClient
                .Setup(c => c.GetAvailableQuantityAsync(1, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);

            SaleLine? addedLine = null;
            _saleLineRepository
                .Setup(r => r.AddAsync(It.IsAny<SaleLine>()))
                .Callback<SaleLine>(l => addedLine = l)
                .ReturnsAsync((SaleLine l) => l);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(Array.Empty<SaleLine>());

            var handler = CreateHandler();
            await handler.Handle(
                new AddSaleLineCommand { SaleId = 1, Barcode = "123", Quantity = 2, ManualDiscountPercent = 20m },
                CancellationToken.None);

            Assert.NotNull(addedLine);
            Assert.Null(addedLine!.ManualDiscountPercent);
            Assert.Equal(42, addedLine.PromotionId);
            // Already-discounted UnitPrice (8) x Quantity (2), no further
            // manual discount applied on top.
            Assert.Equal(16m, addedLine.LineTotal);
        }

        [Fact]
        public async Task Handle_ManualDiscountWithNoPromotion_AppliesDiscountToLineTotal()
        {
            var sale = InProgressSale();
            _saleRepository.Setup(r => r.GetById(1)).ReturnsAsync(sale);
            _warehouseCatalogClient
                .Setup(c => c.ResolveBarcodeAsync("123", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WarehouseItemLookup { ItemId = 1, Sku = "SKU-1", ItemName = "Widget", UnitPrice = 10m });
            _warehouseCatalogClient
                .Setup(c => c.GetAvailableQuantityAsync(1, 1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);

            SaleLine? addedLine = null;
            _saleLineRepository
                .Setup(r => r.AddAsync(It.IsAny<SaleLine>()))
                .Callback<SaleLine>(l => addedLine = l)
                .ReturnsAsync((SaleLine l) => l);
            _saleLineRepository.Setup(r => r.GetBySale(1)).ReturnsAsync(Array.Empty<SaleLine>());

            var handler = CreateHandler();
            var result = await handler.Handle(
                new AddSaleLineCommand { SaleId = 1, Barcode = "123", Quantity = 2, ManualDiscountPercent = 25m },
                CancellationToken.None);

            Assert.Equal(25m, addedLine!.ManualDiscountPercent);
            // 10 x 2 x (1 - 0.25) = 15
            Assert.Equal(15m, addedLine.LineTotal);
            // Sale totals recomputed off the same line (10% tax rate from setup).
            Assert.Equal(15m, sale.NetTotal);
            Assert.Equal(1.5m, sale.TaxAmount);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
