using Warehouse.Application.Features.Stock.Commands.TransferStock;

namespace Warehouse.Application.Tests.Features.Stock.Commands.TransferStock
{
    public class TransferStockCommandValidatorTests
    {
        private readonly TransferStockCommandValidator _validator = new();

        private static TransferStockCommand ValidCommand() => new()
        {
            ItemId = 1,
            FromLocationId = 1,
            ToLocationId = 2,
            Quantity = 5,
        };

        [Fact]
        public void Validate_ValidCommand_Passes()
        {
            var result = _validator.Validate(ValidCommand());

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Validate_SameSourceAndDestinationLocation_Fails()
        {
            var command = ValidCommand();
            command.ToLocationId = command.FromLocationId;

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Fact]
        public void Validate_ZeroOrNegativeQuantity_Fails()
        {
            var command = ValidCommand();
            command.Quantity = 0;

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validate_NonPositiveItemId_Fails(int itemId)
        {
            var command = ValidCommand();
            command.ItemId = itemId;

            var result = _validator.Validate(command);

            Assert.False(result.IsValid);
        }
    }
}
