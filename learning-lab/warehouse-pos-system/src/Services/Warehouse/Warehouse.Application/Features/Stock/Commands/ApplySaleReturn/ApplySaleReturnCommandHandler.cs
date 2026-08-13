using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.ApplySaleReturn
{
    public class ApplySaleReturnCommandHandler : IRequestHandler<ApplySaleReturnCommand, ApplySaleResultDto>
    {
        private readonly IProcessedSaleReturnEventRepository _processedSaleReturnEventRepository;
        private readonly StockAdjustmentStager _stager;
        private readonly IUnitOfWork _unitOfWork;

        public ApplySaleReturnCommandHandler(
            IProcessedSaleReturnEventRepository processedSaleReturnEventRepository,
            StockAdjustmentStager stager,
            IUnitOfWork unitOfWork)
        {
            _processedSaleReturnEventRepository = processedSaleReturnEventRepository;
            _stager = stager;
            _unitOfWork = unitOfWork;
        }

        public async Task<ApplySaleResultDto> Handle(ApplySaleReturnCommand request, CancellationToken cancellationToken)
        {
            // Same idempotent-receiver check as ApplySaleCommand, against
            // its own dedup table — a repeat SaleReturned delivery is a
            // no-op, not a second restock.
            if (await _processedSaleReturnEventRepository.ExistsForSale(request.SaleId))
            {
                return new ApplySaleResultDto { SaleId = request.SaleId, AlreadyProcessed = true };
            }

            // Positive on every line — the mirror image of ApplySaleCommand.
            // createIfMissing isn't needed here: a line being returned was
            // necessarily decremented by ApplySaleCommand first, so its
            // StockLevel row already exists.
            foreach (var line in request.Lines)
            {
                await _stager.Stage(line.ItemId, request.LocationId, line.Quantity, StockTransactionReason.Return, $"Return of Sale {request.SaleId}");
            }

            await _processedSaleReturnEventRepository.AddAsync(new ProcessedSaleReturnEvent { SaleId = request.SaleId });

            await _unitOfWork.SaveChangesAsync();

            return new ApplySaleResultDto { SaleId = request.SaleId, AlreadyProcessed = false };
        }
    }
}
