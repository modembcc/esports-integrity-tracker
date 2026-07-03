using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class MarketLinksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PolymarketClient _polymarket;

    public MarketLinksController(AppDbContext db, PolymarketClient polymarket)
    {
        _db = db;
        _polymarket = polymarket;
    }

    public record CreateLinkRequest(int MatchId, string Slug);

    [HttpPost]
    public async Task<ActionResult> Create(CreateLinkRequest req)
    {
        var match = await _db.Matches.FindAsync(req.MatchId);
        if (match is null) return NotFound($"No match with id {req.MatchId}");

        if (await _db.MarketLinks.AnyAsync(l => l.MatchId == req.MatchId))
            return Conflict($"Match {req.MatchId} is already linked");

        var market = await _polymarket.GetMarketBySlugAsync(req.Slug);
        if (market is null)
            return NotFound($"No Polymarket market for slug '{req.Slug}' (not created yet?)");

        var tokenIds = market.ClobTokenIds();
        if (tokenIds.Count == 0)
            return Problem($"Market '{req.Slug}' returned no clobTokenIds");

        DateTime? gameStart = DateTime.TryParse(market.GameStartTime, null,
            System.Globalization.DateTimeStyles.AdjustToUniversal
                | System.Globalization.DateTimeStyles.AssumeUniversal,
            out var gs) ? gs : null;

        var link = new MarketLink
        {
            MatchId = req.MatchId,
            PolymarketSlug = req.Slug,
            ConditionId = market.ConditionId,
            Question = market.Question,
            ClobTokenIds = tokenIds,
            OutcomeNames = market.Outcomes(),
            GameStartTimeUtc = gameStart,
        };

        _db.MarketLinks.Add(link);
        await _db.SaveChangesAsync();

        // Echo the question so you can verify you linked the right match
        return CreatedAtAction(nameof(Create), new { link.Id, link.Question, link.OutcomeNames });
    }

    [HttpPost("backfill")]
    public async Task<ActionResult> Backfill()
    {
        var links = await _db.MarketLinks.ToListAsync();
        var inserted = 0;

        foreach (var link in links)
        {
            for (var i = 0; i < link.ClobTokenIds.Count; i++)
            {
                var tokenId = link.ClobTokenIds[i];
                var outcome = i < link.OutcomeNames.Count ? link.OutcomeNames[i] : $"outcome_{i}";

                var latest = await _db.PriceSnapshots
                    .Where(s => s.MarketLinkId == link.Id && s.ClobTokenId == tokenId)
                    .MaxAsync(s => (DateTime?)s.CapturedAtUtc);

                long? startTs = latest is null
                    ? null
                    : new DateTimeOffset(latest.Value).ToUnixTimeSeconds() + 1;

                var history = await _polymarket.GetPriceHistoryAsync(tokenId, 10, startTs);

                _db.PriceSnapshots.AddRange(history.Select(p => new PriceSnapshot
                {
                    MarketLinkId = link.Id,
                    ClobTokenId = tokenId,
                    OutcomeName = outcome,
                    Price = p.Price,
                    CapturedAtUtc = p.TimestampUtc,
                    Source = latest is null ? "backfill" : "live",
                }));
                inserted += history.Count;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { inserted });
    }
}