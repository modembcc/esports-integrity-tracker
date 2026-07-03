public class MarketLink
{
    public int Id { get; set; }

    public int MatchId { get; set; }
    public Match Match { get; set; } = null!;

    public string PolymarketSlug { get; set; } = "";
    public string ConditionId { get; set; } = "";
    public string? Question { get; set; }

    public List<string> ClobTokenIds { get; set; } = new();  // index-aligned with OutcomeNames
    public List<string> OutcomeNames { get; set; } = new();  // team names as Polymarket spells them

    public DateTime? GameStartTimeUtc { get; set; }
    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;
}