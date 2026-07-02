public class PandaScoreClient
{
    private readonly HttpClient _http;

    public PandaScoreClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<PandaScoreMatchDto>> GetMatchesForSerieAsync(
        int serieId, CancellationToken ct)
    {
        var url = $"/lol/matches?filter[serie_id]={serieId}&per_page=50";
        var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var matches = await response.Content
            .ReadFromJsonAsync<List<PandaScoreMatchDto>>(cancellationToken: ct);

        return matches ?? new List<PandaScoreMatchDto>();
    }
}