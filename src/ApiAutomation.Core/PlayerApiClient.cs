using System.Net;
using System.Text.Json;
using ApiAutomation.Core.Models;
using RestSharp;

namespace ApiAutomation.Core;

public class PlayerApiClient : IDisposable
{
    private readonly RestClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public List<RequestLogEntry> RequestLog { get; } = new();

    public PlayerApiClient(string baseUrl, HttpMessageHandler? handler = null)
    {
        var options = new RestClientOptions(baseUrl);

        if (handler != null)
        {
            options.ConfigureMessageHandler = _ => handler;
        }

        _client = new RestClient(options);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<string> LoginAsync(string username, string password)
    {
        var request = new RestRequest("/api/tester/login", Method.Post);
        request.AddHeader("Content-Type", "application/json");

        var loginRequest = new LoginRequest
        {
            Username = username,
            Password = password
        };

        var body = JsonSerializer.Serialize(loginRequest, _jsonOptions);
        request.AddStringBody(body, ContentType.Json);

        var response = await _client.ExecuteAsync(request);
        LogRequest("POST", "/api/tester/login", request, body, response);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ApiException(
                response.StatusCode,
                $"Login failed with status {(int)response.StatusCode}",
                response.Content);
        }

        var loginResponse = JsonSerializer.Deserialize<LoginResponse>(response.Content!, _jsonOptions)
            ?? throw new ApiException(response.StatusCode, "Failed to deserialize login response");

        return loginResponse.Token;
    }

    public async Task<PlayerResponse> CreatePlayerAsync(Player player, string authToken)
    {
        var request = new RestRequest("/api/automationTask/create", Method.Post);
        request.AddHeader("Authorization", $"Bearer {authToken}");

        var body = JsonSerializer.Serialize(player, _jsonOptions);
        request.AddStringBody(body, ContentType.Json);

        var response = await _client.ExecuteAsync(request);
        LogRequest("POST", "/api/automationTask/create", request, body, response);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new ApiException(
                response.StatusCode,
                $"Create player failed with status {(int)response.StatusCode}",
                response.Content);
        }

        return JsonSerializer.Deserialize<PlayerResponse>(response.Content!, _jsonOptions)
            ?? throw new ApiException(response.StatusCode, "Failed to deserialize player response");
    }

    public async Task<Player> GetPlayerAsync(string playerId, string authToken)
    {
        var request = new RestRequest("/api/automationTask/getOne", Method.Get);
        request.AddHeader("Authorization", $"Bearer {authToken}");
        request.AddQueryParameter("id", playerId);

        var response = await _client.ExecuteAsync(request);
        LogRequest("GET", $"/api/automationTask/getOne?id={playerId}", request, null, response);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ApiException(
                response.StatusCode,
                $"Get player failed with status {(int)response.StatusCode}",
                response.Content);
        }

        return JsonSerializer.Deserialize<Player>(response.Content!, _jsonOptions)
            ?? throw new ApiException(response.StatusCode, "Failed to deserialize player");
    }

    public async Task<List<Player>> GetAllPlayersAsync(string authToken, bool sortByName = false)
    {
        var request = new RestRequest("/api/automationTask/getAll", Method.Get);
        request.AddHeader("Authorization", $"Bearer {authToken}");

        var response = await _client.ExecuteAsync(request);
        LogRequest("GET", "/api/automationTask/getAll", request, null, response);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new ApiException(
                response.StatusCode,
                $"Get all players failed with status {(int)response.StatusCode}",
                response.Content);
        }

        var players = JsonSerializer.Deserialize<List<Player>>(response.Content!, _jsonOptions)
            ?? throw new ApiException(response.StatusCode, "Failed to deserialize players list");

        if (sortByName)
        {
            players.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        }

        return players;
    }

    public async Task DeletePlayerAsync(string playerId, string authToken)
    {
        var request = new RestRequest($"/api/automationTask/deleteOne/{playerId}", Method.Delete);
        request.AddHeader("Authorization", $"Bearer {authToken}");

        var response = await _client.ExecuteAsync(request);
        LogRequest("DELETE", $"/api/automationTask/deleteOne/{playerId}", request, null, response);

        if (!response.IsSuccessful)
        {
            throw new ApiException(
                response.StatusCode,
                $"Delete player failed with status {(int)response.StatusCode}",
                response.Content);
        }
    }

    private void LogRequest(string method, string url, RestRequest request, string? body, RestResponse response)
    {
        var headers = new Dictionary<string, string>();
        foreach (var header in request.Parameters.Where(p => p.Type == ParameterType.HttpHeader))
        {
            headers[header.Name!] = header.Value?.ToString() ?? string.Empty;
        }

        RequestLog.Add(new RequestLogEntry
        {
            Method = method,
            Url = url,
            RequestHeaders = headers,
            RequestBody = body,
            StatusCode = (int)response.StatusCode,
            ResponseBody = response.Content
        });
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
