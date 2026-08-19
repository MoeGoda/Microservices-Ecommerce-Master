namespace POS.Application.Models
{
    // A read-only mid-shift snapshot of one open CashDrawerSession — it
    // doesn't close the session, just summarizes it so far.
    //
    // SalesTotal is informational only, not folded into
    // ExpectedCashInDrawer: this app has no payment-method/split-tender
    // field on Sale at all (deliberately out of scope, see the POS
    // build-out plan), so there's no way to know how much of SalesTotal
    // was actually paid in cash vs. card. Reporting it as "expected cash"
    // would be a fabricated number dressed up as a real one.
    public class CashDrawerXReportDto
    {
        public int SessionId { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal OpeningFloat { get; set; }
        public decimal CashInTotal { get; set; }
        public decimal CashOutTotal { get; set; }
        public int CompletedSaleCount { get; set; }
        public decimal SalesTotal { get; set; }
        public decimal ExpectedCashInDrawer { get; set; }
    }
}
