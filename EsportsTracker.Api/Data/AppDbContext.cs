using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Odds> Odds => Set<Odds>();
    public DbSet<MarketLink> MarketLinks => Set<MarketLink>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>()
            .HasOne(m => m.Team1)
            .WithMany(t => t.MatchesAsTeam1)
            .HasForeignKey(m => m.Team1Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Team2)
            .WithMany(t => t.MatchesAsTeam2)
            .HasForeignKey(m => m.Team2Id)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .HasOne(m => m.Winner)
            .WithMany()
            .HasForeignKey(m => m.WinnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Match>()
            .Property(m => m.Stage)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .Property(m => m.Status)
            .HasConversion<string>();

        modelBuilder.Entity<MarketLink>()
            .HasIndex(l => l.MatchId).IsUnique();

        modelBuilder.Entity<PriceSnapshot>()
            .HasIndex(s => new { s.MarketLinkId, s.ClobTokenId, s.CapturedAtUtc });
    }
}