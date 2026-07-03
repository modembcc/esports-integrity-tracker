public class PriceSnapshot
{
    public long Id { get; set; }

    public int MarketLinkId { get; set; }
    public MarketLink MarketLink { get; set; } = null!;

    public string ClobTokenId { get; set; } = "";
    public string OutcomeName { get; set; } = "";
    public decimal Price { get; set; }               // implied probability 0.0–1.0
    public DateTime CapturedAtUtc { get; set; }
    public string Source { get; set; } = "live";     // "backfill" | "live"
}