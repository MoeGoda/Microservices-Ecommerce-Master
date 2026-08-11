using Warehouse.Domain.Entities;

namespace Warehouse.Application.Contracts.Persistence
{
    public interface IUnitOfMeasureRepository
    {
        Task<IEnumerable<UnitOfMeasure>> GetAll();
        Task<UnitOfMeasure?> GetById(int id);
        Task<UnitOfMeasure?> GetByCode(string code);
    }
}
