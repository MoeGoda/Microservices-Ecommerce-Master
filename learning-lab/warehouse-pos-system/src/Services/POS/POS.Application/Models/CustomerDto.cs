using POS.Domain.Entities;

namespace POS.Application.Models
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int LoyaltyPoints { get; set; }
        public decimal Balance { get; set; }

        public static CustomerDto FromEntity(Customer customer)
        {
            return new CustomerDto
            {
                Id = customer.Id,
                Name = customer.Name,
                Phone = customer.Phone,
                Email = customer.Email,
                LoyaltyPoints = customer.LoyaltyPoints,
                Balance = customer.Balance,
            };
        }
    }
}
