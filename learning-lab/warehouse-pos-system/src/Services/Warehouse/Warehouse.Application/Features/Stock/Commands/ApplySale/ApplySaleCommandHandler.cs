using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.ApplySale
{
    public class ApplySaleCommandHandler : IRequestHandler<ApplySaleCommand, ApplySaleResultDto>
    {
        private readonly IProcessedSaleEventRepository _processedSaleEventRepository;
        private readonly StockAdjustmentStager _stager;
        private readonly IUnitOfWork _unitOfWork;

        public ApplySaleCommandHandler(
            IProcessedSaleEventRepository processedSaleEventRepository,
            StockAdjustmentStager stager,
            IUnitOfWork unitOfWork)
        {
            _processedSaleEventRepository = processedSaleEventRepository;
            _stager = stager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApplySaleResultDto> Handle(ApplySaleCommand request, CancellationToken cancellationToken)
        {
            // The idempotent-receiver check: at-least-once delivery from
            // POS's outbox (C3) means this exact SaleId can arrive more
            // than once. Treating a repeat as a no-op success — instead
            // of decrementing stock a second time — is what makes
            // retrying safe.
            if (await _processedSaleEventRepository.ExistsForSale(request.SaleId))
            {
                return new ApplySaleResultDto { SaleId = request.SaleId, AlreadyProcessed = true };
            }

            // Negative on every line — this is a sale, stock only ever
            // decreases here. Nothing commits until the single
            // SaveChangesAsync below, so a short-stock failure on any one
            // line rolls back every earlier line's staged change too —
            // one database, one transaction boundary, no per-line
            // compensation needed (see StockAdjustmentStager).
            foreach (var line in request.Lines)
            {
                await _stager.Stage(line.ItemId, request.LocationId, -line.Quantity, StockTransactionReason.Sale, $"Sale {request.SaleId}");
            }

            await _processedSaleEventRepository.AddAsync(new ProcessedSaleEvent { SaleId = request.SaleId });

            await _unitOfWork.SaveChangesAsync();

            return new ApplySaleResultDto { SaleId = request.SaleId, AlreadyProcessed = false };
        }
    }
}
