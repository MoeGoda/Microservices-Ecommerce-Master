using POS.Application.Features.Sales;
using POS.Domain.Entities;
using Xunit;

namespace POS.Application.Tests.Features.Sales
{
    public class SaleTotalsCalculatorTests
    {
        private static SaleLine Line(decimal lineTotal) => new()
        {
            LineTotal = lineTotal,
            UnitPrice = lineTotal,
            Quantity = 1,
        };

        [Fact]
        public void Recompute_NoDiscountsNotTaxExempt_TaxAppliedToFullLineSum()
        {
            var sale = new Sale();
            var lines = new[] { Line(10m), Line(20m) };

            SaleTotalsCalculator.Recompute(sale, lines, taxRatePercent: 10m);

            Assert.Equal(30m, sale.NetTotal);
            Assert.Equal(3m, sale.TaxAmount);
            Assert.Equal(33m, sale.Total);
        }

        [Fact]
        public void Recompute_WithReceiptDiscount_AppliesDiscountBeforeTax()
        {
            var sale = new Sale { ManualReceiptDiscountPercent = 10m };
            var lines = new[] { Line(100m) };

            SaleTotalsCalculator.Recompute(sale, lines, taxRatePercent: 8.5m);

            Assert.Equal(90m, sale.NetTotal);
            Assert.Equal(7.65m, sale.TaxAmount);
            Assert.Equal(97.65m, sale.Total);
        }

        [Fact]
        public void Recompute_TaxExempt_TaxAmountIsZeroRegardlessOfRate()
        {
            var sale = new Sale { IsTaxExempt = true };
            var lines = new[] { Line(50m) };

            SaleTotalsCalculator.Recompute(sale, lines, taxRatePercent: 20m);

            Assert.Equal(50m, sale.NetTotal);
            Assert.Equal(0m, sale.TaxAmount);
            Assert.Equal(50m, sale.Total);
        }

        [Fact]
        public void Recompute_NoLines_TotalsAreZero()
        {
            var sale = new Sale();

            SaleTotalsCalculator.Recompute(sale, Array.Empty<SaleLine>(), taxRatePercent: 8.5m);

            Assert.Equal(0m, sale.NetTotal);
            Assert.Equal(0m, sale.TaxAmount);
            Assert.Equal(0m, sale.Total);
        }

        [Fact]
        public void Recompute_RoundsNetTotalAndTaxToTwoDecimalPlaces()
        {
            var sale = new Sale();
            // 3 lines of 3.33 = 9.99; a 33% receipt discount would produce
            // a repeating decimal (6.6933) if not rounded.
            var lines = new[] { Line(3.33m), Line(3.33m), Line(3.33m) };
            sale.ManualReceiptDiscountPercent = 33m;

            SaleTotalsCalculator.Recompute(sale, lines, taxRatePercent: 8.5m);

            Assert.Equal(6.69m, sale.NetTotal);
            Assert.Equal(0.57m, sale.TaxAmount);
            Assert.Equal(7.26m, sale.Total);
        }

        [Fact]
        public void Recompute_ReceiptDiscountAndTaxExemptTogether_DiscountStillApplies()
        {
            var sale = new Sale { ManualReceiptDiscountPercent = 50m, IsTaxExempt = true };
            var lines = new[] { Line(40m) };

            SaleTotalsCalculator.Recompute(sale, lines, taxRatePercent: 8.5m);

            Assert.Equal(20m, sale.NetTotal);
            Assert.Equal(0m, sale.TaxAmount);
            Assert.Equal(20m, sale.Total);
        }
    }
}
