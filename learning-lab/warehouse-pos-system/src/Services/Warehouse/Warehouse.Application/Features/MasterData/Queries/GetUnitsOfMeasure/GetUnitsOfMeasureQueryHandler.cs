using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.MasterData;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetUnitsOfMeasure
{
    public class GetUnitsOfMeasureQueryHandler : IRequestHandler<GetUnitsOfMeasureQuery, IEnumerable<UnitOfMeasureDto>>
    {
        private const string CacheKey = "warehouse:master-data:units-of-measure";

        private readonly IUnitOfMeasureRepository _unitOfMeasureRepository;
        private readonly MasterDataCache _cache;

        public GetUnitsOfMeasureQueryHandler(IUnitOfMeasureRepository unitOfMeasureRepository, MasterDataCache cache)
        {
            _unitOfMeasureRepository = unitOfMeasureRepository;
            _cache = cache;
        }

        public async Task<IEnumerable<UnitOfMeasureDto>> Handle(GetUnitsOfMeasureQuery request, CancellationToken cancellationToken)
        {
            return await _cache.GetOrSetAsync(CacheKey, async () =>
            {
                var units = await _unitOfMeasureRepository.GetAll();
                return units.Select(UnitOfMeasureDto.FromEntity);
            }, cancellationToken);
        }
    }
}
