// An upset = the team that WON was priced below 0.5 just before the match.
// Inputs: the winner's name (as Polymarket spells it — MarketLink.OutcomeNames),
// the match start time, and the price history. Pure function, no dependencies.

public record UpsetResult(
    string WinnerOutcomeName,
    decimal WinnerPreMatchPrice,   // implied probability just before start
    decimal UpsetMagnitude,        // 0.5 - price; bigger = bigger upset
    DateTime PricedAtUtc);

public class UpsetDetectionService
{
    /// <summary>
    /// Returns null when: match not finished / winner unknown, no pre-match
    /// snapshot for the winner exists, or the favorite simply won.
    /// </summary>
    public UpsetResult? Detect(
        IReadOnlyList<PriceSnapshot> snapshots,
        string? winnerOutcomeName,
        DateTime? gameStartTimeUtc)
    {
        if (snapshots is null || snapshots.Count == 0) return null;
        if (string.IsNullOrWhiteSpace(winnerOutcomeName)) return null;
        if (!gameStartTimeUtc.HasValue) return null;

        // Last observed price for the winning team BEFORE the match started.
        var lastPreMatch = snapshots
            .Where(s => s.OutcomeName == winnerOutcomeName
                     && s.CapturedAtUtc < gameStartTimeUtc.Value)
            .OrderByDescending(s => s.CapturedAtUtc)
            .FirstOrDefault();

        if (lastPreMatch is null) return null;
        if (lastPreMatch.Price >= 0.5m) return null; // favorite won: not an upset

        return new UpsetResult(
            winnerOutcomeName,
            lastPreMatch.Price,
            0.5m - lastPreMatch.Price,
            lastPreMatch.CapturedAtUtc);
    }
}