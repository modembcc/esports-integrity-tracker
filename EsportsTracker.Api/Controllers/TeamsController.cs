using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeamsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Team>>> GetAll() =>
        Ok(await _db.Teams.OrderBy(t => t.Name).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Team>> GetById(int id)
    {
        var team = await _db.Teams.FindAsync(id);
        return team is null ? NotFound() : Ok(team);
    }
}