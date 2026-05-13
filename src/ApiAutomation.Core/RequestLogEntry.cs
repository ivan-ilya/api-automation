namespace ApiAutomation.Core;

public class RequestLogEntry
{
    public string Method { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> RequestHeaders { get; set; } = new();
    public string? RequestBody { get; set; }
    public int StatusCode { get; set; }
    public string? ResponseBody { get; set; }
}
