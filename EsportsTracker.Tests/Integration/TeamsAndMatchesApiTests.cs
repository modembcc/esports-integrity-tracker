using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EsportsTracker.Tests.Integration;

// Exercises the real HTTP pipeline end to end (routing, DbContext, JSON
// shaping) rather than just the pure services DetectionServiceTests covers.
[Collection("Api")]
public class TeamsAndMatchesApiTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public TeamsAndMatchesApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Team> SeedTeamAsync(string name = "Test Team")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var team = new Team
        {
            Name = $"{name} {Guid.NewGuid():N}",
            Region = "LCK",
            ExternalId = Guid.NewGuid().ToString(),
        };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private async Task<Match> SeedMatchAsync(MatchStatus status = MatchStatus.Scheduled)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var team1 = await SeedTeamAsync("Home");
        var team2 = await SeedTeamAsync("Away");

        var match = new Match
        {
            Team1Id = team1.Id,
            Team2Id = team2.Id,
            Status = status,
            ScheduledTime = DateTime.UtcNow.AddHours(2),
            ExternalId = Guid.NewGuid().ToString(),
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        return match;
    }

    [Fact]
    public async Task GetTeamById_ReturnsTeam_WhenItExists()
    {
        var team = await SeedTeamAsync();

        var response = await _client.GetAsync($"/api/teams/{team.Id}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Team>();
        Assert.Equal(team.Name, body!.Name);
    }

    [Fact]
    public async Task GetTeamById_Returns404_WhenMissing()
    {
        var response = await _client.GetAsync("/api/teams/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMatchById_ReturnsShapedPayload_WithTeamNames()
    {
        var match = await SeedMatchAsync();

        var response = await _client.GetAsync($"/api/matches/{match.Id}");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(match.Id, body.GetProperty("id").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("team1").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("team2").GetString()));
    }

    [Fact]
    public async Task GetMatchById_Returns404_WhenMissing()
    {
        var response = await _client.GetAsync("/api/matches/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAllMatches_FilteredByStatus_OnlyReturnsMatchingMatches()
    {
        var scheduled = await SeedMatchAsync(MatchStatus.Scheduled);
        var completed = await SeedMatchAsync(MatchStatus.Completed);

        var response = await _client.GetAsync("/api/matches?status=Completed");
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<List<JsonElement>>();
        var ids = body!.Select(m => m.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(completed.Id, ids);
        Assert.DoesNotContain(scheduled.Id, ids);
    }
}
