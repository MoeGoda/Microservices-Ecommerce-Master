using Common.Exceptions;
using MediatR;
using Reporting.Application.Contracts.Persistence;
using Reporting.Application.Models;
using Reporting.Domain.Entities;

namespace Reporting.Application.Features.Ingestion.Commands.IngestSaleReturned
{
    public class IngestSaleReturnedCommandHandler : IRequestHandler<IngestSaleReturnedCommand, IngestResultDto>
    {
        private readonly ISaleRecordRepository _saleRecordRepository;
        private readonly IUnitOfWork _unitOfWork;

        public IngestSaleReturnedCommandHandler(ISaleRecordRepository saleRecordRepository, IUnitOfWork unitOfWork)
        {
            _saleRecordRepository = saleRecordRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IngestResultDto> Handle(IngestSaleReturnedCommand request, CancellationToken cancellationToken)
        {
            // If SaleReturned somehow arrives before SaleCompleted has
            // been ingested yet (at-least-once delivery gives no ordering
            // guarantee across two separate outbox messages), there's no
            // SaleRecord row to mark returned. Throwing NotFoundException
            // turns into a failed HTTP response, which POS's
            // OutboxDispatcher treats as a retryable failure — the same
            // self-healing path every other consumer here already relies
            // on, not a new mechanism.
            var record = await _saleRecordRepository.GetBySaleId(request.SaleId)
                ?? throw new NotFoundException(nameof(SaleRecord), request.SaleId);

            // The inbox check: a repeat SaleReturned delivery for a sale
            // already marked returned is a no-op.
            if (record.ReturnedAtUtc is not null)
            {
                return new IngestResultDto { AlreadyProcessed = true };
            }

            record.ReturnedAtUtc = DateTime.UtcNow;
            await _saleRecordRepository.UpdateAsync(record);
            await _unitOfWork.SaveChangesAsync();

            return new IngestResultDto { AlreadyProcessed = false };
        }
    }
}
