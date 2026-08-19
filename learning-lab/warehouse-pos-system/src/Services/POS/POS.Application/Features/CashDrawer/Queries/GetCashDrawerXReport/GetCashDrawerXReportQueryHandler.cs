using Common.Exceptions;
using MediatR;
using POS.Application.Contracts.Persistence;
using POS.Application.Models;
using POS.Domain.Entities;

namespace POS.Application.Features.CashDrawer.Queries.GetCashDrawerXReport
{
    public class GetCashDrawerXReportQueryHandler : IRequestHandler<GetCashDrawerXReportQuery, CashDrawerXReportDto>
    {
        private readonly ICashDrawerRepository _cashDrawerRepository;
        private readonly ISaleRepository _saleRepository;

        public GetCashDrawerXReportQueryHandler(ICashDrawerRepository cashDrawerRepository, ISaleRepository saleRepository)
        {
            _cashDrawerRepository = cashDrawerRepository;
            _saleRepository = saleRepository;
        }

        public async Task<CashDrawerXReportDto> Handle(GetCashDrawerXReportQuery request, CancellationToken cancellationToken)
        {
            var session = await _cashDrawerRepository.GetSessionById(request.SessionId)
                ?? throw new NotFoundException(nameof(CashDrawerSession), request.SessionId);

            var movements = (await _cashDrawerRepository.GetMovements(session.Id)).ToList();
            var cashInTotal = movements.Where(m => m.Type == CashMovementType.CashIn).Sum(m => m.Amount);
            var cashOutTotal = movements.Where(m => m.Type == CashMovementType.CashOut).Sum(m => m.Amount);

            var completedSales = (await _saleRepository.GetCompletedSince(session.LocationId, session.OpenedAt)).ToList();

            return new CashDrawerXReportDto
            {
                SessionId = session.Id,
                OpenedAt = session.OpenedAt,
                OpeningFloat = session.OpeningFloat,
                CashInTotal = cashInTotal,
                CashOutTotal = cashOutTotal,
                CompletedSaleCount = completedSales.Count,
                SalesTotal = completedSales.Sum(s => s.Total),
                ExpectedCashInDrawer = session.OpeningFloat + cashInTotal - cashOutTotal,
            };
        }
    }
}
