using System.Text.Json;
using System.Text.Json.Serialization;

// ---------- Gamma API: GET /markets/slug/{slug} ----------
// Response has ~100 fields; we only bind what we use. Verified against
// https://docs.polymarket.com/api-reference/markets/get-market-by-slug

public class PolymarketMarketDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("conditionId")]
    public string ConditionId { get; set; } = "";

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("active")]
    public bool? Active { get; set; }

    [JsonPropertyName("closed")]
    public bool? Closed { get; set; }

    // Match start time for sports markets (string in the schema, not date-time)
    [JsonPropertyName("gameStartTime")]
    public string? GameStartTime { get; set; }

    [JsonPropertyName("volumeNum")]
    public double? VolumeNum { get; set; }

    [JsonPropertyName("liquidityNum")]
    public double? LiquidityNum { get; set; }

    [JsonPropertyName("lastTradePrice")]
    public double? LastTradePrice { get; set; }

    // GOTCHA: these three are JSON-encoded strings INSIDE the JSON,
    // e.g. "outcomes": "[\"T1\", \"Gen.G\"]" — parse via the helpers.
    [JsonPropertyName("outcomes")]
    public string? OutcomesRaw { get; set; }

    [JsonPropertyName("outcomePrices")]
    public string? OutcomePricesRaw { get; set; }

    [JsonPropertyName("clobTokenIds")]
    public string? ClobTokenIdsRaw { get; set; }

    /// <summary>Outcome names, index-aligned with ClobTokenIds(). For a
    /// moneyline market these are the two team names.</summary>
    public List<string> Outcomes() => ParseStringArray(OutcomesRaw);

    /// <summary>Asset IDs — these are what /prices-history wants as `market`.</summary>
    public List<string> ClobTokenIds() => ParseStringArray(ClobTokenIdsRaw);

    public List<decimal> OutcomePrices() =>
        ParseStringArray(OutcomePricesRaw)
            .Select(s => decimal.TryParse(s, out var d) ? d : 0m)
            .ToList();

    private static List<string> ParseStringArray(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(raw) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }
}

// ---------- CLOB API: GET /prices-history ----------
// Verified against https://docs.polymarket.com/api-reference/markets/get-prices-history
// Query params: market (asset id, REQUIRED), startTs, endTs,
//               interval (max|all|1m|1w|1d|6h|1h), fidelity (minutes, default 1)

public class PriceHistoryResponseDto
{
    [JsonPropertyName("history")]
    public List<PricePointDto> History { get; set; } = new();
}

public class PricePointDto
{
    /// <summary>Unix seconds.</summary>
    [JsonPropertyName("t")]
    public long Timestamp { get; set; }

    /// <summary>Implied probability, 0.0–1.0.</summary>
    [JsonPropertyName("p")]
    public decimal Price { get; set; }

    public DateTime TimestampUtc => DateTimeOffset.FromUnixTimeSeconds(Timestamp).UtcDateTime;
}