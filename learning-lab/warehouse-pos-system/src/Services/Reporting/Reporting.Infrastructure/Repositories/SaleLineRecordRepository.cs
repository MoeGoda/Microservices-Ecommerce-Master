using Reporting.Application.Contracts.Persistence;
using Reporting.Domain.Entities;
using Reporting.Infrastructure.Persistence;

namespace Reporting.Infrastructure.Repositories
{
    public class SaleLineRecordRepository : ISaleLineRecordRepository
    {
        private readonly ReportingContext _context;

        public SaleLineRecordRepository(ReportingContext context)
        {
            _context = context;
        }

        public async Task<SaleLineRecord> AddAsync(SaleLineRecord record)
        {
            await _context.SaleLineRecords.AddAsync(record);
            return record;
        }
    }
}
