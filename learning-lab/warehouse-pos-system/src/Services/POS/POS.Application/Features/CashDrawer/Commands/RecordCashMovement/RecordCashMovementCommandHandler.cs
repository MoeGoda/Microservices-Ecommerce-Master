using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.CashDrawer.Commands.RecordCashMovement
{
    public class RecordCashMovementCommandHandler : IRequestHandler<RecordCashMovementCommand, CashMovementDto>
    {
        private readonly ICashDrawerRepository _cashDrawerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public RecordCashMovementCommandHandler(ICashDrawerRepository cashDrawerRepository, IUnitOfWork unitOfWork)
        {
            _cashDrawerRepository = cashDrawerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CashMovementDto> Handle(RecordCashMovementCommand request, CancellationToken cancellationToken)
        {
            var session = await _cashDrawerRepository.GetOpenSession(request.LocationId)
                ?? throw new ConflictException($"Location {request.LocationId} has no open cash drawer session; open one before recording a movement.");

            var movement = new CashMovement
            {
                CashDrawerSessionId = session.Id,
                CashDrawerSession = session,
                Type = request.Type,
                Amount = request.Amount,
                Reason = request.Reason,
                CreatedByUserId = request.CreatedByUserId,
            };

            await _cashDrawerRepository.AddMovementAsync(movement);
            await _unitOfWork.SaveChangesAsync();

            return CashMovementDto.FromEntity(movement);
        }
    }
}
