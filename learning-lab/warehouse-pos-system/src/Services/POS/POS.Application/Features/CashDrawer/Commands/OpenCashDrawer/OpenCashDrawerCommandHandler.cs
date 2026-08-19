using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.CashDrawer.Commands.OpenCashDrawer
{
    public class OpenCashDrawerCommandHandler : IRequestHandler<OpenCashDrawerCommand, CashDrawerSessionDto>
    {
        private readonly ICashDrawerRepository _cashDrawerRepository;
        private readonly IUnitOfWork _unitOfWork;

        public OpenCashDrawerCommandHandler(ICashDrawerRepository cashDrawerRepository, IUnitOfWork unitOfWork)
        {
            _cashDrawerRepository = cashDrawerRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<CashDrawerSessionDto> Handle(OpenCashDrawerCommand request, CancellationToken cancellationToken)
        {
            var existingOpenSession = await _cashDrawerRepository.GetOpenSession(request.LocationId);
            if (existingOpenSession is not null)
            {
                throw new ConflictException($"Location {request.LocationId} already has an open cash drawer session (#{existingOpenSession.Id}); close it before opening another.");
            }

            var session = new CashDrawerSession
            {
                LocationId = request.LocationId,
                CashierUserId = request.CashierUserId,
                OpeningFloat = request.OpeningFloat,
                OpenedAt = DateTime.UtcNow,
            };

            await _cashDrawerRepository.AddSessionAsync(session);
            await _unitOfWork.SaveChangesAsync();

            return CashDrawerSessionDto.FromEntity(session);
        }
    }
}
