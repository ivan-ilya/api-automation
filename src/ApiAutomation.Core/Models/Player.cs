using System.Text.Json.Serialization;

namespace ApiAutomation.Core.Models;

public class Player
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
