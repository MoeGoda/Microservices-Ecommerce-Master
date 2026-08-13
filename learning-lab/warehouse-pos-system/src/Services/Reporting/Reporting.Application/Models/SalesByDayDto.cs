namespace Reporting.Application.Models
{
    // One row per calendar day (UTC) that had at least one completed
    // sale — days with zero sales simply don't appear, rather than every
    // row being padded with zero-days back to some arbitrary start date.
    // The Angular chart fills gaps itself if it ever needs a continuous
    // date axis; the query stays a plain GROUP BY.
    public class SalesByDayDto
    {
        public DateOnly Date { get; set; }
        public int SaleCount { get; set; }
        public decimal Total { get; set; }
    }
}
