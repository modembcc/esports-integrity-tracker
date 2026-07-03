// Tests/DetectionServiceTests.cs  (xUnit)
// If you don't have a test project yet:
//   dotnet new xunit -n EsportsTracker.Tests
//   dotnet add EsportsTracker.Tests reference <your-api-project>.csproj
//   dotnet sln add EsportsTracker.Tests
// The AnomalyDetectionService ctor takes IConfiguration; build a stub inline.

using Microsoft.Extensions.Configuration;
using Xunit;

public class DetectionServiceTests
{
    private static readonly DateTime GameStart = new(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);

    private static PriceSnapshot Snap(string outcome, decimal price, int minutesBeforeStart) =>
        new()
        {
            OutcomeName = outcome,
            Price = price,
            CapturedAtUtc = GameStart.AddMinutes(-minutesBeforeStart),
        };

    private static AnomalyDetectionService Detector(decimal threshold = 0.15m)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AnomalyDetection:ThresholdPoints"] = threshold.ToString(),
            })
            .Build();
        return new AnomalyDetectionService(config);
    }

    [Fact]
    public void Detect_FlagsLargePreMatchSwing()
    {
        var snapshots = new List<PriceSnapshot>
        {
            Snap("T1", 0.60m, 300),
            Snap("T1", 0.58m, 240),
            Snap("T1", 0.40m, 120), // 20-point drop from opening 0.60
            Snap("T1", 0.41m, 60),
        };

        var results = Detector().Detect(snapshots, GameStart);

        var anomaly = Assert.Single(results);
        Assert.Equal("T1", anomaly.OutcomeName);
        Assert.Equal(0.60m, anomaly.FromPrice);
        Assert.Equal(0.40m, anomaly.ToPrice);
        Assert.Equal(0.20m, anomaly.Shift);
    }

    [Fact]
    public void Detect_IgnoresGentleDrift()
    {
        var snapshots = new List<PriceSnapshot>
        {
            Snap("T1", 0.55m, 300),
            Snap("T1", 0.52m, 200),
            Snap("T1", 0.50m, 100),
            Snap("T1", 0.48m, 30), // total drift 0.07 < 0.15
        };

        Assert.Empty(Detector().Detect(snapshots, GameStart));
    }

    [Fact]
    public void Detect_IgnoresInMatchMovement()
    {
        // Prices resolving toward 1.0 DURING the match must not be flagged.
        var snapshots = new List<PriceSnapshot>
        {
            Snap("T1", 0.55m, 60),
            Snap("T1", 0.80m, -30),  // 30 min AFTER start
            Snap("T1", 0.98m, -120), // match nearly decided
        };

        Assert.Empty(Detector().Detect(snapshots, GameStart));
    }

    [Fact]
    public void Detect_HandlesEmptyAndSingleSnapshot()
    {
        var detector = Detector();
        Assert.Empty(detector.Detect(new List<PriceSnapshot>(), GameStart));
        Assert.Empty(detector.Detect(new List<PriceSnapshot> { Snap("T1", 0.5m, 60) }, GameStart));
    }

    [Fact]
    public void UpsetDetect_FlagsUnderdogWin()
    {
        var snapshots = new List<PriceSnapshot>
        {
            Snap("Team Secret Whales", 0.30m, 120),
            Snap("Team Secret Whales", 0.28m, 15), // last pre-match price
            Snap("Hanwha Life Esports", 0.72m, 15),
        };

        var result = new UpsetDetectionService()
            .Detect(snapshots, "Team Secret Whales", GameStart);

        Assert.NotNull(result);
        Assert.Equal(0.28m, result!.WinnerPreMatchPrice);
        Assert.Equal(0.22m, result.UpsetMagnitude);
    }

    [Fact]
    public void UpsetDetect_ReturnsNullWhenFavoriteWins()
    {
        var snapshots = new List<PriceSnapshot>
        {
            Snap("Hanwha Life Esports", 0.72m, 15),
        };

        Assert.Null(new UpsetDetectionService()
            .Detect(snapshots, "Hanwha Life Esports", GameStart));
    }
}