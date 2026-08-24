using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AGC.Server.Data;

/// <summary>
/// Lets EF Core's migrations tooling create an AppDbContext without booting the full
/// app (which requires OWNER_EMAIL etc. to be set) — design-time only. Generating a
/// migration only needs the provider (to emit correct provider-specific SQL) and a
/// syntactically valid connection string; it never actually connects, so this
/// placeholder never needs to point at a real database.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseNpgsql("Host=localhost;Database=agc_design;Username=postgres;Password=postgres");
        return new AppDbContext(builder.Options);
    }
}
