public class PolymarketClient
{
    private readonly HttpClient _gamma;
    private readonly HttpClient _clob;

    public PolymarketClient(IHttpClientFactory factory)
    {
        _gamma = factory.CreateClient("polymarket-gamma");
        _clob = factory.CreateClient("polymarket-clob");
    }

    /// <summary>
    /// Resolve a market from its slug (the last URL segment on polymarket.com).
    /// Returns null on 404 — i.e. a typo'd or not-yet-created slug.
    /// </summary>
    public async Task<PolymarketMarketDto?> GetMarketBySlugAsync(
        string slug, CancellationToken ct = default)
    {
        var response = await _gamma.GetAsync($"/markets/slug/{Uri.EscapeDataString(slug)}", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PolymarketMarketDto>(cancellationToken: ct);
    }

    /// <summary>
    /// Price history for ONE outcome token.
    /// NOTE: `market` here is the CLOB token (asset) id from clobTokenIds —
    /// NOT the conditionId and NOT the slug. Passing the wrong id returns
    /// an empty history, not an error, so double-check if you get nothing back.
    /// </summary>
    /// <param name="fidelityMinutes">Data resolution in minutes (API default is 1).
    /// 10 keeps row counts sane for multi-day pre-match windows.</param>
    public async Task<List<PricePointDto>> GetPriceHistoryAsync(
        string clobTokenId,
        int fidelityMinutes = 10,
        long? startTsUnixSeconds = null,
        CancellationToken ct = default)
    {
        var url = $"/prices-history?market={clobTokenId}&fidelity={fidelityMinutes}";

        // startTs lets the periodic job fetch only what's new;
        // omit it (interval=max) for the initial backfill.
        url += startTsUnixSeconds.HasValue
            ? $"&startTs={startTsUnixSeconds.Value}"
            : "&interval=max";

        var response = await _clob.GetFromJsonAsync<PriceHistoryResponseDto>(url, ct);
        return response?.History ?? new();
    }
}