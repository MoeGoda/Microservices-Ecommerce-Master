using POS.Domain.Entities;

namespace POS.Application.Models
{
    public class CashMovementDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Reason { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public static CashMovementDto FromEntity(CashMovement movement)
        {
            return new CashMovementDto
            {
                Id = movement.Id,
                Type = movement.Type.ToString(),
                Amount = movement.Amount,
                Reason = movement.Reason,
                CreatedAt = movement.CreatedAt,
            };
        }
    }
}
