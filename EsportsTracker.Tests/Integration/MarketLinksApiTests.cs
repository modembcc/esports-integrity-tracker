using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace EsportsTracker.Tests.Integration;

[Collection("Api")]
public class MarketLinksApiTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public MarketLinksApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // No PolymarketClient call should happen here — the controller checks
    // the match exists before ever touching the Gamma API.
    [Fact]
    public async Task Create_Returns404_WhenMatchDoesNotExist()
    {
        var response = await _client.PostAsJsonAsync("/api/marketlinks", new
        {
            MatchId = 999999999,
            Slug = "does-not-matter",
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/marketlinks");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Delete_Returns404_WhenLinkDoesNotExist()
    {
        var response = await _client.DeleteAsync("/api/marketlinks/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
