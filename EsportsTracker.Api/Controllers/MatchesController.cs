using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MatchesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult> GetAll([FromQuery] MatchStatus? status)
    {
        var query = _db.Matches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .AsQueryable();

        if (status is not null)
            query = query.Where(m => m.Status == status);

        // Scheduled: soonest first (next match on top).
        // Everything else: most recent first.
        query = status == MatchStatus.Scheduled
            ? query.OrderBy(m => m.ScheduledTime)
            : query.OrderByDescending(m => m.ScheduledTime);

        var matches = await query
            .Select(m => new
            {
                m.Id,
                Team1 = m.Team1.Name,
                Team2 = m.Team2.Name,
                m.Status,
                m.ScheduledTime,
                Winner = m.Winner != null ? m.Winner.Name : null
            })
            .ToListAsync();

        return Ok(matches);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> GetById(int id)
    {
        var match = await _db.Matches
            .Include(m => m.Team1)
            .Include(m => m.Team2)
            .Include(m => m.Winner)
            .FirstOrDefaultAsync(m => m.Id == id);

        return match is null ? NotFound() : Ok(new
        {
            match.Id,
            Team1 = match.Team1.Name,
            Team2 = match.Team2.Name,
            match.Status,
            match.ScheduledTime,
            Winner = match.Winner?.Name
        });
    }
}