using System.Text.Json;
using Allure.Net.Commons;
using ApiAutomation.Core;
using ApiAutomation.Core.Models;
using ApiAutomation.Tests.Services;
using ApiAutomation.Tests.TestData;
using FluentAssertions;

namespace ApiAutomation.Tests.Tests;

public class EndToEndTests : IDisposable
{
    private readonly PlayerApiClient _client;
    private readonly PlayerApiService _api;
    private readonly string _authToken;
    private static readonly JsonSerializerOptions JsonPretty = new() { WriteIndented = true };

    public EndToEndTests()
    {
        var handler = new FakeHttpHandler();
        _client = new PlayerApiClient("http://localhost", handler);
        _api = new PlayerApiService(handler);
        _authToken = PlayerFaker.GenerateToken();
    }

    [Fact]
    public async Task Login_ReturnsValidToken()
    {
        _api.Login(_authToken);
        var (username, password) = PlayerFaker.GenerateCredentials();

        AllureApi.Step($"Login as '{username}'");
        var token = await _client.LoginAsync(username, password);

        AllureApi.Step("Verify token is valid");
        token.Should().Be(_authToken);
    }

    [Fact]
    public async Task Create12Players_EachReturnsCreatedWithValidId()
    {
        _api.CreatePlayer();

        AllureApi.Step("Create 12 players");
        for (int i = 0; i < 12; i++)
        {
            var newPlayer = PlayerFaker.GenerateNewPlayer();
            var response = await _client.CreatePlayerAsync(newPlayer, _authToken);
            response.Player.Id.Should().NotBeNullOrEmpty();
        }

        AllureApi.Step("Verify 12 requests sent");
        _client.RequestLog.Count.Should().Be(12);
    }

    [Fact]
    public async Task GetPlayerProfile_ReturnsCorrectData()
    {
        var expectedPlayer = new Player
        {
            Id = Guid.NewGuid().ToString(),
            Name = PlayerFaker.GenerateNewPlayer().Name
        };
        _api.GetPlayer(expectedPlayer);

        AllureApi.Step($"Get player profile '{expectedPlayer.Id}'");
        var result = await _client.GetPlayerAsync(expectedPlayer.Id, _authToken);

        AllureApi.Step("Verify player data matches");
        result.Id.Should().Be(expectedPlayer.Id);
        result.Name.Should().Be(expectedPlayer.Name);
    }

    [Fact]
    public async Task GetAllPlayers_SortedByName_ReturnsAlphabeticalOrder()
    {
        var names = PlayerFaker.GeneratePlayerNames(12);
        _api.GetAllPlayers(names);

        AllureApi.Step("Get all players sorted by name");
        var result = await _client.GetAllPlayersAsync(_authToken, sortByName: true);

        AllureApi.Step("Verify alphabetical order");
        result.Should().HaveCount(12);
        result.Should().BeInAscendingOrder(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAllCreatedPlayers_EachReturnsSuccess()
    {
        var playerIds = Enumerable.Range(1, 12).Select(_ => Guid.NewGuid().ToString()).ToList();
        _api.DeletePlayers(playerIds);

        AllureApi.Step("Delete all 12 players");
        foreach (var id in playerIds)
        {
            await _client.DeletePlayerAsync(id, _authToken);
        }

        AllureApi.Step("Verify all deleted");
        _client.RequestLog.Count.Should().Be(12);
    }

    public void Dispose()
    {
        if (_client.RequestLog.Count > 0)
        {
            var json = JsonSerializer.Serialize(_client.RequestLog, JsonPretty);
            AllureApi.AddAttachment("HTTP Log", "application/json",
                System.Text.Encoding.UTF8.GetBytes(json), "json");
        }

        _client.Dispose();
    }
}
