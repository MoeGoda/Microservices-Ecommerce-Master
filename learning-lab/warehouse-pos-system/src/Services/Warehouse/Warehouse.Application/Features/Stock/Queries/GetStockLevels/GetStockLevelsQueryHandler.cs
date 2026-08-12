using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Queries.GetStockLevels
{
    public class GetStockLevelsQueryHandler : IRequestHandler<GetStockLevelsQuery, IEnumerable<StockLevelDto>>
    {
        private readonly IItemRepository _itemRepository;
        private readonly IStockLevelRepository _stockLevelRepository;

        public GetStockLevelsQueryHandler(IItemRepository itemRepository, IStockLevelRepository stockLevelRepository)
        {
            _itemRepository = itemRepository;
            _stockLevelRepository = stockLevelRepository;
        }

        public async Task<IEnumerable<StockLevelDto>> Handle(GetStockLevelsQuery request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var stockLevels = await _stockLevelRepository.GetByItem(item.Id);
            return stockLevels.Select(s => StockLevelDto.FromEntity(s, item.BaseUnitOfMeasure.Code));
        }
    }
}
