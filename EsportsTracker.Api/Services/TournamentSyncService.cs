using Microsoft.EntityFrameworkCore;

public class TournamentSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PandaScoreClient _pandaScore;
    private readonly ILogger<TournamentSyncService> _logger;
    private readonly List<PandaScoreLeagueOptions> _leagues;

    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(15);

    public TournamentSyncService(
        IServiceScopeFactory scopeFactory,
        PandaScoreClient pandaScore,
        IConfiguration config,
        ILogger<TournamentSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _pandaScore = pandaScore;
        _logger = logger;
        _leagues = config.GetSection("PandaScore:Leagues").Get<List<PandaScoreLeagueOptions>>()
            ?? new List<PandaScoreLeagueOptions>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Phase 1: matches/teams from PandaScore
            try
            {
                await SyncMatchesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PandaScore sync failed, will retry next interval");
            }

            // Phase 2: price history from Polymarket.
            // Separate try/catch: one source being down must not block the other.
            try
            {
                await SyncPricesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Polymarket price sync failed, will retry next interval");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }

    // ─────────────────────────── Phase 1: PandaScore ───────────────────────────

    private async Task SyncMatchesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        foreach (var leagueConfig in _leagues)
        {
            try
            {
                await SyncLeagueMatchesAsync(db, leagueConfig, ct);
            }
            catch (Exception ex)
            {
                // One bad serie id shouldn't stop the rest of the leagues from syncing.
                _logger.LogError(ex, "PandaScore sync failed for league {League} (serie {SerieId})",
                    leagueConfig.Name, leagueConfig.SerieId);
            }
        }
    }

    private async Task SyncLeagueMatchesAsync(
        AppDbContext db, PandaScoreLeagueOptions leagueConfig, CancellationToken ct)
    {
        var league = await UpsertLeagueAsync(db, leagueConfig, ct);

        var matches = await _pandaScore.GetMatchesForSerieAsync(leagueConfig.SerieId, ct);
        _logger.LogInformation("Fetched {Count} matches from PandaScore for {League}",
            matches.Count, leagueConfig.Name);

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

            match.LeagueId = league.Id;
            match.Team1Id = team1.Id;
            match.Team2Id = team2.Id;
            match.ScheduledTime = dto.ScheduledAt ?? DateTime.UtcNow;
            match.Status = MapStatus(dto.Status);
            match.WinnerId = dto.WinnerId is null
                ? null
                : (team1.ExternalId == dto.WinnerId.ToString() ? team1.Id : team2.Id);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<League> UpsertLeagueAsync(
        AppDbContext db, PandaScoreLeagueOptions config, CancellationToken ct)
    {
        var league = await db.Leagues
            .FirstOrDefaultAsync(l => l.PandaScoreSerieId == config.SerieId, ct);

        if (league is null)
        {
            league = new League { Name = config.Name, PandaScoreSerieId = config.SerieId };
            db.Leagues.Add(league);
            await db.SaveChangesAsync(ct); // save now so league.Id is available
        }
        else
        {
            league.Name = config.Name; // keep name in sync with config
        }

        return league;
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

    // ─────────────────────────── Phase 2: Polymarket ───────────────────────────

    private async Task SyncPricesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // PolymarketClient is registered scoped → resolve inside the scope,
        // unlike PandaScoreClient which is constructor-injected (typed client).
        var polymarket = scope.ServiceProvider.GetRequiredService<PolymarketClient>();

        // Skip settled matches: their history is final, no point re-polling.
        var links = await db.MarketLinks
            .Include(l => l.Match)
            .Where(l => l.SettledAtUtc == null)
            .ToListAsync(ct);

        if (links.Count == 0)
        {
            _logger.LogInformation("No active market links to sync");
            return;
        }

        foreach (var link in links)
        {
            try
            {
                var newPoints = await FetchNewPricePointsAsync(db, polymarket, link, ct);
                _logger.LogInformation(
                    "MarketLink {LinkId} ({Slug}): {Count} new price points",
                    link.Id, link.PolymarketSlug, newPoints);
                if (link.Match.Status == MatchStatus.Completed)
                {
                    var hasAnyHistory = newPoints > 0 ||
                        await db.PriceSnapshots.AnyAsync(s => s.MarketLinkId == link.Id, ct);

                    if (hasAnyHistory)
                    {
                        link.SettledAtUtc = DateTime.UtcNow;
                        _logger.LogInformation("MarketLink {LinkId} settled, final history captured", link.Id);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "MarketLink {LinkId} match is Completed but no price history exists — not settling, check token ids",
                            link.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                // One bad link (dead market, wrong token id) shouldn't stop the rest
                _logger.LogError(ex, "Price sync failed for MarketLink {LinkId}", link.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task<int> FetchNewPricePointsAsync(
        AppDbContext db, PolymarketClient polymarket, MarketLink link, CancellationToken ct)
    {
        var totalNew = 0;

        for (var i = 0; i < link.ClobTokenIds.Count; i++)
        {
            var tokenId = link.ClobTokenIds[i];
            var outcomeName = i < link.OutcomeNames.Count ? link.OutcomeNames[i] : $"Outcome {i}";

            // Only fetch what's newer than what we already have for this token.
            // First run: latest is null → interval=max backfill.
            var latest = await db.PriceSnapshots
                .Where(s => s.MarketLinkId == link.Id && s.ClobTokenId == tokenId)
                .MaxAsync(s => (DateTime?)s.CapturedAtUtc, ct);

            long? startTs = latest is null
                ? null
                : new DateTimeOffset(latest.Value, TimeSpan.Zero).ToUnixTimeSeconds() + 1;

            var history = await polymarket.GetPriceHistoryAsync(
                tokenId, startTsUnixSeconds: startTs, ct: ct);

            foreach (var point in history)
            {
                db.PriceSnapshots.Add(new PriceSnapshot
                {
                    MarketLinkId = link.Id,
                    ClobTokenId = tokenId,
                    OutcomeName = outcomeName,
                    Price = point.Price,
                    CapturedAtUtc = point.TimestampUtc,
                    Source = latest is null ? "backfill" : "live",
                });
            }

            totalNew += history.Count;
        }

        return totalNew;
    }
}