using Common.Exceptions;
using MediatR;
using Warehouse.Application.Contracts.Persistence;
using Warehouse.Application.Exceptions;
using Warehouse.Application.Models;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Features.Stock.Commands.AdjustStock
{
    public class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, StockLevelDto>
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IStockLevelRepository _stockLevelRepository;
        private readonly IStockTransactionRepository _stockTransactionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public AdjustStockCommandHandler(
            IItemRepository itemRepository,
            ILocationRepository locationRepository,
            IStockLevelRepository stockLevelRepository,
            IStockTransactionRepository stockTransactionRepository,
            IUnitOfWork unitOfWork)
        {
            _itemRepository = itemRepository;
            _locationRepository = locationRepository;
            _stockLevelRepository = stockLevelRepository;
            _stockTransactionRepository = stockTransactionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<StockLevelDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
        {
            var item = await _itemRepository.GetById(request.ItemId)
                ?? throw new NotFoundException(nameof(Item), request.ItemId);

            var location = await _locationRepository.GetById(request.LocationId)
                ?? throw new NotFoundException(nameof(Location), request.LocationId);

            var stockLevel = await _stockLevelRepository.GetByItemAndLocation(item.Id, location.Id)
                ?? throw new NotFoundException(nameof(StockLevel), $"item {item.Id}, location {location.Id}");

            var newQuantity = stockLevel.QuantityOnHand + request.QuantityChange;
            if (newQuantity < 0)
            {
                throw new InsufficientStockException(item.Name, location.Name, stockLevel.QuantityOnHand, request.QuantityChange);
            }

            stockLevel.QuantityOnHand = newQuantity;
            await _stockLevelRepository.UpdateAsync(stockLevel);

            var transaction = new StockTransaction
            {
                ItemId = item.Id,
                LocationId = location.Id,
                QuantityChange = request.QuantityChange,
                Reason = StockTransactionReason.Adjustment,
                Reference = request.Reference,
            };
            await _stockTransactionRepository.AddAsync(transaction);

            await _unitOfWork.SaveChangesAsync();

            return StockLevelDto.FromEntity(stockLevel, item.BaseUnitOfMeasure.Code);
        }
    }
}
