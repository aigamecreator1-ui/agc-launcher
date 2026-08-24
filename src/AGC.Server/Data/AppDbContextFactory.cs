using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AGC.Server.Data;

/// <summary>
/// Lets EF Core's migrations tooling create an AppDbContext without booting the full
/// app (which requires OWNER_EMAIL etc. to be set) — design-time only.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseSqlite("Data Source=agc.db");
        return new AppDbContext(builder.Options);
    }
}
