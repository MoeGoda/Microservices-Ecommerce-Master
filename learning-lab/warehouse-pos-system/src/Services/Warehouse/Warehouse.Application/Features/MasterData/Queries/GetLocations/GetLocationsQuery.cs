using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetLocations
{
    public class GetLocationsQuery : IRequest<IEnumerable<LocationDto>>
    {
    }
}
