using Microsoft.EntityFrameworkCore;
using POS.Application.Contracts.Persistence;
using POS.Domain.Entities;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly PosContext _context;

        public CustomerRepository(PosContext context)
        {
            _context = context;
        }

        public async Task<Customer?> GetById(int id)
        {
            return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<Customer>> Search(string? search, int page, int pageSize)
        {
            var query = _context.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
            }

            return await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> CountSearch(string? search)
        {
            var query = _context.Customers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search) || (c.Phone != null && c.Phone.Contains(search)));
            }

            return await query.CountAsync();
        }

        public async Task<Customer> AddAsync(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            return customer;
        }

        public Task UpdateAsync(Customer customer)
        {
            _context.Customers.Update(customer);
            return Task.CompletedTask;
        }
    }
}
