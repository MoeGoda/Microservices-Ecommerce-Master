using POS.Domain.Entities;

namespace POS.Application.Contracts.Persistence
{
    public interface ICustomerRepository
    {
        Task<Customer?> GetById(int id);

        // Name/phone, case-insensitive substring — the same "search box
        // over a short field, no full-text engine" shape every other
        // admin list screen in this app already uses.
        Task<IEnumerable<Customer>> Search(string? search, int page, int pageSize);
        Task<int> CountSearch(string? search);

        Task<Customer> AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
    }
}
