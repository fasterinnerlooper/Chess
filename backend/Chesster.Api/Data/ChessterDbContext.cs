using Microsoft.EntityFrameworkCore;
using Chesster.Api.Models;

namespace Chesster.Api.Data;

public class ChessterDbContext : DbContext
{
    public ChessterDbContext(DbContextOptions<ChessterDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserLogin> UserLogins => Set<UserLogin>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameAnalysis> GameAnalyses => Set<GameAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasIndex(e => new { e.Provider, e.ProviderKey }).IsUnique();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasOne(g => g.User)
                .WithMany(u => u.Games)
                .HasForeignKey(g => g.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<GameAnalysis>(entity =>
        {
            entity.HasOne(a => a.Game)
                .WithMany(g => g.Analyses)
                .HasForeignKey(a => a.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.GameId);
        });
    }
}