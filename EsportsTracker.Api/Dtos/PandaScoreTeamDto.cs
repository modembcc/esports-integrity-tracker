using System.Text.Json.Serialization;

public class PandaScoreTeamDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}