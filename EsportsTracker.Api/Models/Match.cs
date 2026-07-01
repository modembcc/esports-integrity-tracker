public class Match
{
    public int Id { get; set; }

    public int Team1Id { get; set; }
    public Team Team1 { get; set; } = null!;

    public int Team2Id { get; set; }
    public Team Team2 { get; set; } = null!;

    public Stage Stage { get; set; }
    public DateTime ScheduledTime { get; set; }
    public MatchStatus Status { get; set; }

    public int? WinnerId { get; set; }
    public Team? Winner { get; set; }

    public string? ExternalId { get; set; }

    public ICollection<Odds> Odds { get; set; } = new List<Odds>();
}