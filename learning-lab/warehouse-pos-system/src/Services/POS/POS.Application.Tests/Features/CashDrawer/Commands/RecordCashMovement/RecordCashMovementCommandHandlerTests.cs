using Common.Exceptions;
using Moq;
using POS.Application.Contracts.Persistence;
using POS.Application.Features.CashDrawer.Commands.RecordCashMovement;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.CashDrawer.Commands
{
    public class RecordCashMovementCommandHandlerTests
    {
        private readonly Mock<ICashDrawerRepository> _cashDrawerRepository = new();
        private readonly Mock<IUnitOfWork> _unitOfWork = new();

        private RecordCashMovementCommandHandler CreateHandler() => new(
            _cashDrawerRepository.Object,
            _unitOfWork.Object);

        [Fact]
        public async Task Handle_NoOpenSessionAtLocation_ThrowsConflictException()
        {
            _cashDrawerRepository.Setup(r => r.GetOpenSession(1)).ReturnsAsync((CashDrawerSession?)null);
            var handler = CreateHandler();

            await Assert.ThrowsAsync<ConflictException>(() =>
                handler.Handle(
                    new RecordCashMovementCommand { LocationId = 1, Type = CashMovementType.CashIn, Amount = 20m, Reason = "Change fund", CreatedByUserId = 1 },
                    CancellationToken.None));
        }

        [Fact]
        public async Task Handle_OpenSessionExists_RecordsMovementAgainstIt()
        {
            var session = new CashDrawerSession { Id = 5, LocationId = 1 };
            _cashDrawerRepository.Setup(r => r.GetOpenSession(1)).ReturnsAsync(session);
            CashMovement? added = null;
            _cashDrawerRepository
                .Setup(r => r.AddMovementAsync(It.IsAny<CashMovement>()))
                .Callback<CashMovement>(m => added = m)
                .ReturnsAsync((CashMovement m) => m);

            var handler = CreateHandler();
            var result = await handler.Handle(
                new RecordCashMovementCommand { LocationId = 1, Type = CashMovementType.CashOut, Amount = 15m, Reason = "Petty cash", CreatedByUserId = 9 },
                CancellationToken.None);

            Assert.NotNull(added);
            Assert.Equal(5, added!.CashDrawerSessionId);
            Assert.Equal(CashMovementType.CashOut, added.Type);
            Assert.Equal(15m, added.Amount);
            Assert.Equal("Petty cash", added.Reason);
            Assert.Equal(9, added.CreatedByUserId);
            Assert.Equal(15m, result.Amount);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }
    }
}
