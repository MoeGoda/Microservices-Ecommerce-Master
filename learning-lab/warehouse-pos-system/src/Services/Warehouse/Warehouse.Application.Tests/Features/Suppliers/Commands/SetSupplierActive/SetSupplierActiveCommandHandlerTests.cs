using Common.Exceptions;
using Moq;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.Suppliers.Commands.SetSupplierActive;
using Warehouse.Application.Tests.TestSupport;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Suppliers.Commands.SetSupplierActive
{
    public class SetSupplierActiveCommandHandlerTests
    {
        [Fact]
        public async Task Handle_DeactivatingAnActiveSupplier_FlipsIsActiveAndCommits()
        {
            var supplierRepository = new Mock<ISupplierRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var supplier = TestEntities.Supplier(isActive: true);

            supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);

            var handler = new SetSupplierActiveCommandHandler(supplierRepository.Object, unitOfWork.Object);
            var result = await handler.Handle(new SetSupplierActiveCommand { SupplierId = supplier.Id, IsActive = false }, CancellationToken.None);

            Assert.False(result.IsActive);
            supplierRepository.Verify(r => r.UpdateAsync(supplier), Times.Once);
            unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task Handle_ReactivatingADeactivatedSupplier_FlipsIsActiveBackOn()
        {
            var supplierRepository = new Mock<ISupplierRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();
            var supplier = TestEntities.Supplier(isActive: false);

            supplierRepository.Setup(r => r.GetById(supplier.Id)).ReturnsAsync(supplier);

            var handler = new SetSupplierActiveCommandHandler(supplierRepository.Object, unitOfWork.Object);
            var result = await handler.Handle(new SetSupplierActiveCommand { SupplierId = supplier.Id, IsActive = true }, CancellationToken.None);

            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task Handle_UnknownSupplier_ThrowsNotFoundException()
        {
            var supplierRepository = new Mock<ISupplierRepository>();
            var unitOfWork = new Mock<IUnitOfWork>();

            supplierRepository.Setup(r => r.GetById(999)).ReturnsAsync((Supplier?)null);

            var handler = new SetSupplierActiveCommandHandler(supplierRepository.Object, unitOfWork.Object);

            await Assert.ThrowsAsync<NotFoundException>(
                () => handler.Handle(new SetSupplierActiveCommand { SupplierId = 999, IsActive = false }, CancellationToken.None));
        }
    }
}
