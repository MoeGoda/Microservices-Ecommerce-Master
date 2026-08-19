using POS.Domain.Entities;

namespace POS.Application.Features.Sales
{
    // Every handler that changes a line or a discount/tax-exempt flag
    // (AddSaleLine, RemoveSaleLine, SetLineDiscount, SetReceiptDiscount,
    // SetTaxExempt) needs to re-derive NetTotal/TaxAmount/Total the same
    // way — pulled out once here rather than duplicated five times, the
    // same "one place, not five copies" reasoning F1's compression and
    // the Angular paginateClientSide util already used.
    public static class SaleTotalsCalculator
    {
        public static void Recompute(Sale sale, IEnumerable<SaleLine> lines, decimal taxRatePercent)
        {
            var lineSum = lines.Sum(l => l.LineTotal);
            var receiptDiscountPercent = sale.ManualReceiptDiscountPercent ?? 0m;
            var netTotal = lineSum * (1 - receiptDiscountPercent / 100m);
            var taxAmount = sale.IsTaxExempt ? 0m : netTotal * (taxRatePercent / 100m);

            sale.NetTotal = Math.Round(netTotal, 2);
            sale.TaxAmount = Math.Round(taxAmount, 2);
            sale.Total = sale.NetTotal + sale.TaxAmount;
        }
    }
}
