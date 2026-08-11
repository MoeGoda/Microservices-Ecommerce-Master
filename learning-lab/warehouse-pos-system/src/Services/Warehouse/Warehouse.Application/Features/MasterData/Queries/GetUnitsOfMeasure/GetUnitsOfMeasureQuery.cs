using MediatR;
using Warehouse.Application.Models;

namespace Warehouse.Application.Features.MasterData.Queries.GetUnitsOfMeasure
{
    public class GetUnitsOfMeasureQuery : IRequest<IEnumerable<UnitOfMeasureDto>>
    {
    }
}
