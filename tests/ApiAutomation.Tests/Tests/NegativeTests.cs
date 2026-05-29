using System.Net;
using System.Text.Json;
using Allure.Net.Commons;
using ApiAutomation.Core;
using ApiAutomation.Core.Models;
using ApiAutomation.Tests.Services;
using ApiAutomation.Tests.TestData;
using FluentAssertions;

namespace ApiAutomation.Tests.Tests;

public class NegativeTests : IDisposable
{
    private readonly PlayerApiClient _client;
    private readonly FakeHttpHandler _handler;
    private readonly string _authToken;
    private static readonly JsonSerializerOptions JsonPretty = new() { WriteIndented = true };

    public NegativeTests()
    {
        _handler = new FakeHttpHandler();
        _client = new PlayerApiClient("http://localhost", _handler);
        _authToken = PlayerFaker.GenerateToken();
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        _handler.SetupResponse("/api/tester/login", HttpMethod.Post, HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(new { message = "Invalid username or password" }));

        var (username, password) = PlayerFaker.GenerateCredentials();

        AllureApi.Step($"Attempt login with invalid credentials '{username}'");
        var act = () => _client.LoginAsync(username, password);

        AllureApi.Step("Verify 401 Unauthorized");
        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreatePlayer_WithoutToken_Returns401()
    {
        _handler.SetupResponse("/api/automationTask/create", HttpMethod.Post, HttpStatusCode.Unauthorized,
            JsonSerializer.Serialize(new { message = "Authorization token is missing" }));

        var player = PlayerFaker.GenerateNewPlayer();

        AllureApi.Step("Attempt to create player without valid token");
        var act = () => _client.CreatePlayerAsync(player, "invalid-token");

        AllureApi.Step("Verify 401 Unauthorized");
        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPlayer_UnknownId_Returns404()
    {
        var unknownId = Guid.NewGuid().ToString();
        _handler.SetupResponse("/api/automationTask/getOne", HttpMethod.Get, HttpStatusCode.NotFound,
            JsonSerializer.Serialize(new { message = $"Player with id '{unknownId}' not found" }));

        AllureApi.Step($"Attempt to get player with unknown ID '{unknownId}'");
        var act = () => _client.GetPlayerAsync(unknownId, _authToken);

        AllureApi.Step("Verify 404 Not Found");
        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePlayer_AlreadyDeleted_Returns404()
    {
        var deletedId = Guid.NewGuid().ToString();
        _handler.SetupResponse($"/api/automationTask/deleteOne/{deletedId}", HttpMethod.Delete,
            HttpStatusCode.NotFound,
            JsonSerializer.Serialize(new { message = $"Player with id '{deletedId}' not found" }));

        AllureApi.Step($"Attempt to delete already-deleted player '{deletedId}'");
        var act = () => _client.DeletePlayerAsync(deletedId, _authToken);

        AllureApi.Step("Verify 404 Not Found");
        var exception = await act.Should().ThrowAsync<ApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
