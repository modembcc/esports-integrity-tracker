using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/marketlinks")]
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

    /// <summary>
    /// Link a match to a Polymarket market by slug (last URL segment on
    /// polymarket.com). Resolves the market via Gamma and stores the CLOB
    /// token ids + outcome names the price sync needs.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateLinkRequest req)
    {
        var match = await _db.Matches.FindAsync(req.MatchId);
        if (match is null)
            return NotFound(new { message = $"Match {req.MatchId} not found" });

        var existing = await _db.MarketLinks
            .FirstOrDefaultAsync(l => l.MatchId == req.MatchId);
        if (existing is not null)
            return Conflict(new { message = "Match already linked", linkId = existing.Id });

        var market = await _polymarket.GetMarketBySlugAsync(req.Slug);
        if (market is null)
            return NotFound(new { message = $"No Polymarket market for slug '{req.Slug}'" });

        var tokenIds = market.ClobTokenIds();
        var outcomes = market.Outcomes();
        if (tokenIds.Count == 0)
            return UnprocessableEntity(new { message = "Market has no CLOB token ids" });

        var link = new MarketLink
        {
            MatchId = req.MatchId,
            PolymarketSlug = req.Slug,
            ConditionId = market.ConditionId,
            Question = market.Question,
            ClobTokenIds = tokenIds,
            OutcomeNames = outcomes,
            GameStartTimeUtc = DateTime.TryParse(
                market.GameStartTime,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal
                    | System.Globalization.DateTimeStyles.AssumeUniversal,
                out var gst) ? gst : null,
        };

        _db.MarketLinks.Add(link);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { }, new
        {
            link.Id,
            link.MatchId,
            link.PolymarketSlug,
            link.OutcomeNames,
            link.GameStartTimeUtc,
        });
    }

    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var links = await _db.MarketLinks
            .Include(l => l.Match).ThenInclude(m => m.Team1)
            .Include(l => l.Match).ThenInclude(m => m.Team2)
            .Select(l => new
            {
                l.Id,
                l.MatchId,
                Team1 = l.Match.Team1.Name,
                Team2 = l.Match.Team2.Name,
                l.PolymarketSlug,
                l.OutcomeNames,
                l.GameStartTimeUtc,
            })
            .ToListAsync();

        return Ok(links);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var link = await _db.MarketLinks.FindAsync(id);
        if (link is null) return NotFound();

        _db.MarketLinks.Remove(link);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}