using System.Text.Json.Serialization;

namespace ApiAutomation.Core.Models;

public class LoginResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;
}
