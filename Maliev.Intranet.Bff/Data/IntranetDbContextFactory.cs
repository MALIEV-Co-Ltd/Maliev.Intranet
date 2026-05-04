using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.Intranet.Bff.Data;

/// <summary>
/// Design-time factory used by EF Core tooling to create Intranet migrations.
/// </summary>
public sealed class IntranetDbContextFactory : IDesignTimeDbContextFactory<IntranetDbContext>
{
    /// <inheritdoc />
    public IntranetDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("IntranetDbContext")
            ?? "Host=localhost;Port=5432;Database=intranet_app_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new IntranetDbContext(options);
    }
}
