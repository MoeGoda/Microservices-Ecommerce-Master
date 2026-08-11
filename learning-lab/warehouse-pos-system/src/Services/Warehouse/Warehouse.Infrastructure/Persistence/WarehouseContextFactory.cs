using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warehouse.Infrastructure.Persistence
{
    // Identity's migrations were generated with `--startup-project
    // Identity.API`, because that's where the real DbContextOptions (with
    // the actual connection string from appsettings.json) got configured.
    // Warehouse.API doesn't exist yet — it's Step B3 — so there's no
    // startup project to point `dotnet ef` at.
    //
    // IDesignTimeDbContextFactory<T> is EF Core's answer to exactly this:
    // it's a factory the `dotnet ef` CLI discovers and uses ONLY at design
    // time (generating/applying migrations), completely independent of how
    // the real app constructs WarehouseContext at runtime. The connection
    // string below is never read by the running API — Warehouse.API will
    // configure its own DbContextOptions from its own appsettings.json,
    // exactly like Identity.API does.
    public class WarehouseContextFactory : IDesignTimeDbContextFactory<WarehouseContext>
    {
        public WarehouseContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<WarehouseContext>();
            optionsBuilder.UseSqlServer(
                "Server=localhost,1433;Database=WarehouseDb;User Id=sa;Password=SwN12345678;TrustServerCertificate=True");

            return new WarehouseContext(optionsBuilder.Options);
        }
    }
}
