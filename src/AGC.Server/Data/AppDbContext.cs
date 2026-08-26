using AGC.Server.Entities;
using Microsoft.EntityFrameworkCore;

namespace AGC.Server.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<VerificationCode> VerificationCodes => Set<VerificationCode>();

    public DbSet<Game> Games => Set<Game>();

    public DbSet<Ownership> Ownerships => Set<Ownership>();

    public DbSet<Transaction> Transactions => Set<Transaction>();

    public DbSet<Payout> Payouts => Set<Payout>();

    public DbSet<GameVote> GameVotes => Set<GameVote>();

    public DbSet<GameComment> GameComments => Set<GameComment>();

    public DbSet<GameEngagementEvent> GameEngagementEvents => Set<GameEngagementEvent>();

    public DbSet<LauncherOpenEvent> LauncherOpenEvents => Set<LauncherOpenEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<VerificationCode>(entity =>
        {
            entity.HasIndex(v => v.Email);
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(g => g.PriceUsd).HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Ownership>(entity =>
        {
            entity.HasIndex(o => new { o.UserId, o.GameId }).IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.AmountUsd).HasColumnType("decimal(10,2)");
            entity.HasIndex(t => t.StripeCheckoutSessionId).IsUnique();
        });

        modelBuilder.Entity<Payout>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(12,2)");
            entity.HasIndex(p => p.StripePayoutId).IsUnique();
        });

        modelBuilder.Entity<GameVote>(entity =>
        {
            entity.HasIndex(v => new { v.GameId, v.UserId }).IsUnique();
        });

        modelBuilder.Entity<GameComment>(entity =>
        {
            entity.HasIndex(c => c.GameId);
        });

        modelBuilder.Entity<GameEngagementEvent>(entity =>
        {
            entity.HasIndex(e => new { e.GameId, e.Kind });
        });

        modelBuilder.Entity<LauncherOpenEvent>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        // Every DateTime/DateTime? in this model is a Kind=Utc value (DateTime.UtcNow).
        // Npgsql's default mapping is "timestamp without time zone", which throws at
        // runtime on a Kind=Utc value — SQLite was silently lenient about this, Postgres
        // is not. Map every one to "timestamptz" instead, which accepts Kind=Utc directly.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetColumnType("timestamptz");
                }
            }
        }
    }
}
