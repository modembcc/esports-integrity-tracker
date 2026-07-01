public class Odds
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;

    public decimal Value { get; set; }        // decimal odds, e.g. 1.85
    public DateTime RecordedAt { get; set; }
    public required string Source { get; set; }
}