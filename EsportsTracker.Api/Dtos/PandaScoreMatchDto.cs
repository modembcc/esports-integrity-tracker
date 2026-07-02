using System.Text.Json.Serialization;

public class PandaScoreMatchDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";   // "not_started", "running", "finished"

    [JsonPropertyName("winner_id")]
    public long? WinnerId { get; set; }

    [JsonPropertyName("opponents")]
    public List<PandaScoreOpponentDto> Opponents { get; set; } = new();
}

public class PandaScoreOpponentDto
{
    [JsonPropertyName("opponent")]
    public PandaScoreTeamDto? Opponent { get; set; }
}

