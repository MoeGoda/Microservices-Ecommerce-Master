using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Features.MasterData;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetLocations
{
    public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, IEnumerable<LocationDto>>
    {
        private const string CacheKey = "warehouse:master-data:locations";

        private readonly ILocationRepository _locationRepository;
        private readonly MasterDataCache _cache;

        public GetLocationsQueryHandler(ILocationRepository locationRepository, MasterDataCache cache)
        {
            _locationRepository = locationRepository;
            _cache = cache;
        }

        public async Task<IEnumerable<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            return await _cache.GetOrSetAsync(CacheKey, async () =>
            {
                var locations = await _locationRepository.GetAll();
                return locations.Select(LocationDto.FromEntity);
            }, cancellationToken);
        }
    }
}
