using System.Text.Json.Serialization;

namespace ApiAutomation.Core.Models;

public class PlayerResponse
{
    [JsonPropertyName("player")]
    public Player Player { get; set; } = new();
}
