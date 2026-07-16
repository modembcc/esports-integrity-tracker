public class League
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int PandaScoreSerieId { get; set; }

    public ICollection<Match> Matches { get; set; } = new List<Match>();
}
