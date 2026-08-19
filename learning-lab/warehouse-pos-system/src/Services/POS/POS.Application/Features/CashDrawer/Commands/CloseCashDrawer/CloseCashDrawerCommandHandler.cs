using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.CashDrawer.Commands.CloseCashDrawer
{
    public class CloseCashDrawerCommandHandler : IRequestHandler<CloseCashDrawerCommand, CashDrawerSessionDto>
    {
        private readonly ICashDrawerRepository _cashDrawerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CloseCashDrawerCommandHandler(ICashDrawerRepository cashDrawerRepository, IUnitOfWork unitOfWork)
        {
            _cashDrawerRepository = cashDrawerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CashDrawerSessionDto> Handle(CloseCashDrawerCommand request, CancellationToken cancellationToken)
        {
            var session = await _cashDrawerRepository.GetSessionById(request.SessionId)
                ?? throw new NotFoundException(nameof(CashDrawerSession), request.SessionId);

            if (session.ClosedAt.HasValue)
            {
                throw new ConflictException($"Cash drawer session {session.Id} is already closed.");
            }

            session.ClosedAt = DateTime.UtcNow;
            session.ClosingCount = request.ClosingCount;

            await _cashDrawerRepository.UpdateSessionAsync(session);
            await _unitOfWork.SaveChangesAsync();

            return CashDrawerSessionDto.FromEntity(session);
        }
    }
}
