using System.Text.Json;
using Allure.Net.Commons;
using ApiAutomation.Core;
using ApiAutomation.Core.Models;
using ApiAutomation.Tests.Services;
using ApiAutomation.Tests.TestData;
using FluentAssertions;

namespace ApiAutomation.Tests.Tests;

/// <summary>
/// End-to-end tests for Player API CRUD operations.
/// 
/// Cleanup strategy:
/// - In mock mode (FakeHttpHandler): tests are fully isolated, no shared state.
/// - In real API mode: created player IDs are tracked in <see cref="_createdPlayerIds"/>
///   and deleted in <see cref="Dispose"/> to prevent orphaned test data.
/// </summary>
public class EndToEndTests : IDisposable
{
    private readonly PlayerApiClient _client;
    private readonly PlayerApiService _api;
    private readonly string _authToken;
    private readonly List<string> _createdPlayerIds = new();
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

        AllureApi.Step("Verify token is non-empty string");
        token.Should().NotBeNullOrEmpty("login must return a valid token");
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

            // Schema validation: verify response structure
            response.Should().NotBeNull();
            response.Player.Should().NotBeNull("response must contain a 'player' object");
            response.Player.Id.Should().NotBeNullOrEmpty("created player must have an ID");

            _createdPlayerIds.Add(response.Player.Id!);
        }

        AllureApi.Step("Verify 12 requests sent");
        _client.RequestLog.Count.Should().Be(12);
        _createdPlayerIds.Should().HaveCount(12);
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

        AllureApi.Step("Verify response schema and data");
        result.Should().NotBeNull();
        result.Id.Should().NotBeNullOrEmpty("player must have an ID field");
        result.Name.Should().NotBeNullOrEmpty("player must have a Name field");
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

        AllureApi.Step("Verify response schema");
        result.Should().NotBeNull();
        result.Should().HaveCount(12);
        result.Should().AllSatisfy(player =>
        {
            player.Id.Should().NotBeNullOrEmpty("each player must have an ID");
            player.Name.Should().NotBeNullOrEmpty("each player must have a Name");
        });

        AllureApi.Step("Verify alphabetical order");
        result.Should().BeInAscendingOrder(p => p.Name, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAllCreatedPlayers_EachReturnsSuccess()
    {
        // In a real scenario, these IDs would come from _createdPlayerIds
        // populated during create tests. For isolated mock tests, we generate them.
        var playerIds = Enumerable.Range(1, 12).Select(_ => Guid.NewGuid().ToString()).ToList();
        _api.DeletePlayers(playerIds);

        AllureApi.Step("Delete all 12 players");
        foreach (var id in playerIds)
        {
            await _client.DeletePlayerAsync(id, _authToken);
        }

        AllureApi.Step("Verify all deleted successfully");
        _client.RequestLog.Count.Should().Be(12);
        _client.RequestLog.Should().AllSatisfy(log =>
            log.StatusCode.Should().BeOneOf(200, 204));
    }

    public void Dispose()
    {
        // Attach HTTP log for Allure reporting
        if (_client.RequestLog.Count > 0)
        {
            var json = JsonSerializer.Serialize(_client.RequestLog, JsonPretty);
            AllureApi.AddAttachment("HTTP Log", "application/json",
                System.Text.Encoding.UTF8.GetBytes(json), "json");
        }

        // Cleanup: delete any players created during this test run.
        // In mock mode this is a no-op since FakeHttpHandler is stateless.
        // In real API mode, this ensures no orphaned test data remains.
        if (_createdPlayerIds.Count > 0)
        {
            foreach (var id in _createdPlayerIds)
            {
                try
                {
                    _client.DeletePlayerAsync(id, _authToken).GetAwaiter().GetResult();
                }
                catch
                {
                    // Best-effort cleanup — don't fail the test on teardown errors
                }
            }
        }

        _client.Dispose();
    }
}
