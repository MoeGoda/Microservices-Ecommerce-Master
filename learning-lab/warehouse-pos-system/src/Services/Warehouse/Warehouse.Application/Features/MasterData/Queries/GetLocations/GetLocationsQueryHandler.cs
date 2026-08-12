using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetLocations
{
    public class GetLocationsQueryHandler : IRequestHandler<GetLocationsQuery, IEnumerable<LocationDto>>
    {
        private readonly ILocationRepository _locationRepository;

        public GetLocationsQueryHandler(ILocationRepository locationRepository)
        {
            _locationRepository = locationRepository;
        }

        public async Task<IEnumerable<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
        {
            var locations = await _locationRepository.GetAll();
            return locations.Select(LocationDto.FromEntity);
        }
    }
}
