using System.Net;
using System.Text.Json;
using ApiAutomation.Core.Models;
using ApiAutomation.Tests;

namespace ApiAutomation.Tests.Services;

public class PlayerApiService
{
    private readonly FakeHttpHandler _handler;

    public PlayerApiService(FakeHttpHandler handler)
    {
        _handler = handler;
    }

    public void Login(string token)
    {
        _handler.SetupResponse("/api/tester/login", HttpMethod.Post, HttpStatusCode.OK,
            JsonSerializer.Serialize(new { token }));
    }

    public void CreatePlayer()
    {
        _handler.SetupResponse("/api/automationTask/create", HttpMethod.Post, HttpStatusCode.Created,
            JsonSerializer.Serialize(new { player = new { id = Guid.NewGuid().ToString(), name = "CreatedPlayer" } }));
    }

    public void GetPlayer(Player player)
    {
        _handler.SetupResponse("/api/automationTask/getOne", HttpMethod.Get, HttpStatusCode.OK,
            JsonSerializer.Serialize(new { id = player.Id, name = player.Name }));
    }

    public void GetAllPlayers(List<string> names)
    {
        var players = names.Select(name => new { id = Guid.NewGuid().ToString(), name }).ToList();
        _handler.SetupResponse("/api/automationTask/getAll", HttpMethod.Get, HttpStatusCode.OK,
            JsonSerializer.Serialize(players));
    }

    public void DeletePlayers(List<string> playerIds)
    {
        foreach (var id in playerIds)
            _handler.SetupResponse($"/api/automationTask/deleteOne/{id}", HttpMethod.Delete, HttpStatusCode.OK, "");
    }
}
