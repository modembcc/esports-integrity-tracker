// Controllers/InsightsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/matches")]
public class InsightsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AnomalyDetectionService _anomalies;
    private readonly UpsetDetectionService _upsets;

    public InsightsController(
        AppDbContext db,
        AnomalyDetectionService anomalies,
        UpsetDetectionService upsets)
    {
        _db = db;
        _anomalies = anomalies;
        _upsets = upsets;
    }

    [HttpGet("anomalies")]
    public async Task<ActionResult> GetAnomalies()
    {
        var (links, snapshotsByLink) = await LoadLinkedDataAsync();

        var response = links
            .Select(link => new
            {
                MatchId = link.MatchId,
                Team1 = link.Match.Team1.Name,
                Team2 = link.Match.Team2.Name,
                link.Match.ScheduledTime,
                Anomalies = _anomalies.Detect(
                    snapshotsByLink.GetValueOrDefault(link.Id, new List<PriceSnapshot>()),
                    link.GameStartTimeUtc),
            })
            .Where(x => x.Anomalies.Count > 0)
            .ToList();

        return Ok(response);
    }

    [HttpGet("upsets")]
    public async Task<ActionResult> GetUpsets()
    {
        var (links, snapshotsByLink) = await LoadLinkedDataAsync();

        var response = links
            .Where(l => l.Match.Winner != null)
            .Select(link => new
            {
                MatchId = link.MatchId,
                Team1 = link.Match.Team1.Name,
                Team2 = link.Match.Team2.Name,
                Winner = link.Match.Winner!.Name,
                Upset = _upsets.Detect(
                    snapshotsByLink.GetValueOrDefault(link.Id, new List<PriceSnapshot>()),
                    // Winner name must match Polymarket's spelling. If your
                    // PandaScore team names differ, map via link.OutcomeNames here.
                    ResolveOutcomeName(link, link.Match.Winner!.Name),
                    link.GameStartTimeUtc),
            })
            .Where(x => x.Upset is not null)
            .OrderByDescending(x => x.Upset!.UpsetMagnitude)
            .ToList();

        return Ok(response);
    }

    /// <summary>
    /// PandaScore and Polymarket may spell a team differently ("Gen.G" vs
    /// "Gen.G Esports"). Match loosely against the market's outcome names.
    /// </summary>
    private static string? ResolveOutcomeName(MarketLink link, string pandaScoreName)
    {
        var exact = link.OutcomeNames
            .FirstOrDefault(o => string.Equals(o, pandaScoreName, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;

        return link.OutcomeNames.FirstOrDefault(o =>
            o.Contains(pandaScoreName, StringComparison.OrdinalIgnoreCase) ||
            pandaScoreName.Contains(o, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<(List<MarketLink>, Dictionary<int, List<PriceSnapshot>>)> LoadLinkedDataAsync()
    {
        var links = await _db.MarketLinks
            .Include(l => l.Match).ThenInclude(m => m.Team1)
            .Include(l => l.Match).ThenInclude(m => m.Team2)
            .Include(l => l.Match).ThenInclude(m => m.Winner)
            .ToListAsync();

        var linkIds = links.Select(l => l.Id).ToList();

        var snapshotsByLink = (await _db.PriceSnapshots
                .Where(s => linkIds.Contains(s.MarketLinkId))
                .ToListAsync())
            .GroupBy(s => s.MarketLinkId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return (links, snapshotsByLink);
    }
}