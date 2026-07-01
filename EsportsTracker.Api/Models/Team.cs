public class Team
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Region { get; set; }       // LCK, LPL, LEC, LCS, LCP, CBLOL
    public string? ExternalId { get; set; }

    public ICollection<Match> MatchesAsTeam1 { get; set; } = new List<Match>();
    public ICollection<Match> MatchesAsTeam2 { get; set; } = new List<Match>();
}