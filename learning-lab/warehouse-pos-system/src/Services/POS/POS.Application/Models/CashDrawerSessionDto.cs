using POS.Domain.Entities;

namespace POS.Application.Models
{
    public class CashDrawerSessionDto
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public int CashierUserId { get; set; }
        public decimal OpeningFloat { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal? ClosingCount { get; set; }

        public static CashDrawerSessionDto FromEntity(CashDrawerSession session)
        {
            return new CashDrawerSessionDto
            {
                Id = session.Id,
                LocationId = session.LocationId,
                CashierUserId = session.CashierUserId,
                OpeningFloat = session.OpeningFloat,
                OpenedAt = session.OpenedAt,
                ClosedAt = session.ClosedAt,
                ClosingCount = session.ClosingCount,
            };
        }
    }
}
