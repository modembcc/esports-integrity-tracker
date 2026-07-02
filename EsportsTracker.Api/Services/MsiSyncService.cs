using Microsoft.EntityFrameworkCore;

public class MsiSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PandaScoreClient _client;
    private readonly ILogger<MsiSyncService> _logger;
    private readonly int _serieId;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(15);

    public MsiSyncService(
        IServiceScopeFactory scopeFactory,
        PandaScoreClient client,
        IConfiguration config,
        ILogger<MsiSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _client = client;
        _logger = logger;
        _serieId = config.GetValue<int>("PandaScore:SerieId");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Never let one failed sync kill the background service
                _logger.LogError(ex, "MSI sync failed, will retry next interval");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var matches = await _client.GetMatchesForSerieAsync(_serieId, ct);
        _logger.LogInformation("Fetched {Count} matches from PandaScore", matches.Count);

        foreach (var dto in matches)
        {
            if (dto.Opponents.Count < 2) continue; // TBD matchups not yet decided

            var team1 = await UpsertTeamAsync(db, dto.Opponents[0].Opponent!, ct);
            var team2 = await UpsertTeamAsync(db, dto.Opponents[1].Opponent!, ct);

            var externalId = dto.Id.ToString();
            var match = await db.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, ct);

            if (match is null)
            {
                match = new Match { ExternalId = externalId };
                db.Matches.Add(match);
            }

            match.Team1Id = team1.Id;
            match.Team2Id = team2.Id;
            match.ScheduledTime = dto.ScheduledAt ?? DateTime.UtcNow;
            match.Status = MapStatus(dto.Status);
            match.Stage = Stage.PlayIn; // TODO: derive from tournament data later
            match.WinnerId = dto.WinnerId is null
                ? null
                : (team1.ExternalId == dto.WinnerId.ToString() ? team1.Id : team2.Id);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Team> UpsertTeamAsync(
        AppDbContext db, PandaScoreTeamDto dto, CancellationToken ct)
    {
        var externalId = dto.Id.ToString();
        var team = await db.Teams
            .FirstOrDefaultAsync(t => t.ExternalId == externalId, ct);

        if (team is null)
        {
            team = new Team
            {
                Name = dto.Name,
                Region = dto.Location ?? "Unknown",
                ExternalId = externalId
            };
            db.Teams.Add(team);
            await db.SaveChangesAsync(ct); // save now so team.Id is available
        }
        else
        {
            team.Name = dto.Name; // keep names fresh if PandaScore updates them
        }

        return team;
    }

    private static MatchStatus MapStatus(string psStatus) => psStatus switch
    {
        "running" => MatchStatus.Live,
        "finished" => MatchStatus.Completed,
        _ => MatchStatus.Scheduled
    };
}