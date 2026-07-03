// Detects suspicious PRE-MATCH price movement: any deviation from the
// opening price exceeding ThresholdPoints, using only snapshots captured
// before gameStartTimeUtc.

public record AnomalyResult(
    string OutcomeName,
    decimal FromPrice,
    decimal ToPrice,
    decimal Shift,
    DateTime FromTimeUtc,
    DateTime ToTimeUtc);

public class AnomalyDetectionService
{
    private readonly decimal _thresholdPoints;

    public AnomalyDetectionService(IConfiguration config)
    {
        _thresholdPoints = config.GetValue("AnomalyDetection:ThresholdPoints", 0.15m);
    }

    /// <summary>
    /// Returns one result per outcome that breached the threshold pre-match.
    /// Empty list = nothing suspicious (or not enough data). Never throws
    /// on empty/unsorted/mixed-outcome input.
    /// </summary>
    public List<AnomalyResult> Detect(
        IReadOnlyList<PriceSnapshot> snapshots,
        DateTime? gameStartTimeUtc)
    {
        var results = new List<AnomalyResult>();
        if (snapshots is null || snapshots.Count == 0) return results;

        // Pre-match only. If we don't know the start time, analyze everything
        // we have (better than silently analyzing nothing).
        var preMatch = gameStartTimeUtc.HasValue
            ? snapshots.Where(s => s.CapturedAtUtc < gameStartTimeUtc.Value)
            : snapshots;

        foreach (var group in preMatch.GroupBy(s => s.OutcomeName))
        {
            var ordered = group.OrderBy(s => s.CapturedAtUtc).ToList();
            if (ordered.Count < 2) continue; // can't measure movement from one point

            var opening = ordered[0];

            // First breach wins; early-out keeps it O(n).
            foreach (var point in ordered.Skip(1))
            {
                var shift = Math.Abs(point.Price - opening.Price);
                if (shift <= _thresholdPoints) continue;

                results.Add(new AnomalyResult(
                    group.Key,
                    opening.Price,
                    point.Price,
                    shift,
                    opening.CapturedAtUtc,
                    point.CapturedAtUtc));
                break;
            }
        }

        return results;
    }
}