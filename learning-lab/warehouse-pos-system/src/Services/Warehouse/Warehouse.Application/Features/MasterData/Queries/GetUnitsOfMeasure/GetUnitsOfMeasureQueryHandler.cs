using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetUnitsOfMeasure
{
    public class GetUnitsOfMeasureQueryHandler : IRequestHandler<GetUnitsOfMeasureQuery, IEnumerable<UnitOfMeasureDto>>
    {
        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;

        public GetUnitsOfMeasureQueryHandler(IUnitOfMeasureRepository unitOfMeasureRepository)
        {
            _unitOfMeasureRepository = unitOfMeasureRepository;
        }

        public async Task<IEnumerable<UnitOfMeasureDto>> Handle(GetUnitsOfMeasureQuery request, CancellationToken cancellationToken)
        {
            var units = await _unitOfMeasureRepository.GetAll();
            return units.Select(UnitOfMeasureDto.FromEntity);
        }
    }
}
