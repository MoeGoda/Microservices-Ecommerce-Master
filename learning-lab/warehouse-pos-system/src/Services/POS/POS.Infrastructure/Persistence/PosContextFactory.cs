using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace POS.Infrastructure.Persistence
{
    // Same reasoning as WarehouseContextFactory (Warehouse, B1): there's no
    // POS.API yet to point `dotnet ef` at via --startup-project, since C1
    // is domain/application/infrastructure only. This factory is only ever
    // used at design time by the `dotnet ef` CLI; the real POS.API,
    // whenever it's built, configures its own DbContextOptions from its
    // own appsettings.json, exactly like Warehouse.API does.
    public class PosContextFactory : IDesignTimeDbContextFactory<PosContext>
    {
        public PosContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PosContext>();
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=PosDb;User Id=sa;Password=SwN12345678;TrustServerCertificate=True");

            return new PosContext(optionsBuilder.Options);
        }
    }
}
