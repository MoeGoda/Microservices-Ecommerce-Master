using Common.Exceptions;
using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.CashDrawer.Commands.OpenCashDrawer;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.CashDrawer.Commands
{
    public class OpenCashDrawerCommandHandlerTests
    {
        private readonly Mock<ICashDrawerRepository> _cashDrawerRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private OpenCashDrawerCommandHandler CreateHandler() => new(
            _cashDrawerRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_LocationAlreadyHasOpenSession_ThrowsConflictException()
        {
            _cashDrawerRepository
                .Setup(r => r.GetOpenSession(1))
                .ReturnsAsync(new CashDrawerSession { Id = 5, LocationId = 1 });
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(new OpenCashDrawerCommand { LocationId = 1, CashierUserId = 1, OpeningFloat = 100m }, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoOpenSession_OpensNewSessionWithOpeningFloat()
        {
            _cashDrawerRepository.Setup(r => r.GetOpenSession(1)).ReturnsAsync((CashDrawerSession?)null);
            CashDrawerSession? added = null;
            _cashDrawerRepository
                .Setup(r => r.AddSessionAsync(It.IsAny<CashDrawerSession>()))
                .Callback<CashDrawerSession>(s => added = s)
                .ReturnsAsync((CashDrawerSession s) => s);

            var handler = CreateHandler();
            var result = await handler.Handle(
                new OpenCashDrawerCommand { LocationId = 1, CashierUserId = 7, OpeningFloat = 100m },
                CancellationToken.None);

            Assert.NotNull(added);
            Assert.Equal(1, added!.LocationId);
            Assert.Equal(7, added.CashierUserId);
            Assert.Equal(100m, added.OpeningFloat);
            Assert.Equal(100m, result.OpeningFloat);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
