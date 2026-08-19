using Warehouse.Application.Features.Items.Commands.CreatePromotion;
using Warehouse.Domain.Entities;

namespace Warehouse.Application.Tests.Features.Items.Commands.CreatePromotion
{
    public class CreatePromotionCommandValidatorTests
    {
        private readonly CreatePromotionCommandValidator _validator = new();

        private static CreatePromotionCommand ValidCommand() => new()
        {
            ItemId = 1,
            DiscountType = DiscountType.PercentageOff,
            DiscountValue = 10,
            StartsAtUtc = new DateTime(2026, 1, 1),
            EndsAtUtc = new DateTime(2026, 1, 31),
        };

        [Fact]
        public void Validate_ValidPercentagePromotion_Passes()
        {
            Assert.True(_validator.Validate(ValidCommand()).IsValid);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(50)]
        public void Validate_PercentageOffAtOrBelow100_Passes(decimal value)
        {
            var command = ValidCommand();
            command.DiscountValue = value;

            Assert.True(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Validate_PercentageOffAbove100_Fails()
        {
            // Paying the customer to take it isn't a real discount.
            var command = ValidCommand();
            command.DiscountValue = 100.01m;

            Assert.False(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Validate_FixedAmountOffAbove1Million_Fails()
        {
            var command = ValidCommand();
            command.DiscountType = DiscountType.FixedAmountOff;
            command.DiscountValue = 1_000_001;

            Assert.False(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Validate_FixedAmountOffAtOneMillion_Passes()
        {
            var command = ValidCommand();
            command.DiscountType = DiscountType.FixedAmountOff;
            command.DiscountValue = 1_000_000;

            Assert.True(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Validate_FixedAmountOffAbove100_StillPasses()
        {
            // The 100 cap only makes sense for a percentage — a fixed
            // currency discount bigger than 100 is completely ordinary.
            var command = ValidCommand();
            command.DiscountType = DiscountType.FixedAmountOff;
            command.DiscountValue = 5000;

            Assert.True(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Validate_EndsAtUtcNotAfterStartsAtUtc_Fails()
        {
            var command = ValidCommand();
            command.EndsAtUtc = command.StartsAtUtc;

            Assert.False(_validator.Validate(command).IsValid);
        }

        [Fact]
        public void Validate_ZeroOrNegativeDiscountValue_Fails()
        {
            var command = ValidCommand();
            command.DiscountValue = 0;

            Assert.False(_validator.Validate(command).IsValid);
        }
    }
}
