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
    }
}
