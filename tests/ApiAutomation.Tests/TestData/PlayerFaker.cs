using ApiAutomation.Core.Models;
using Bogus;

namespace ApiAutomation.Tests.TestData;

public static class PlayerFaker
{
    private static readonly Faker _faker = new();

    public static Player GenerateNewPlayer() => new()
    {
        Name = _faker.Person.FirstName
    };

    public static List<Player> GenerateNewPlayers(int count) =>
        Enumerable.Range(0, count).Select(_ => GenerateNewPlayer()).ToList();

    public static List<string> GeneratePlayerNames(int count) =>
        Enumerable.Range(0, count).Select(_ => _faker.Person.FirstName).ToList();

    public static (string Username, string Password) GenerateCredentials() =>
        (_faker.Internet.UserName(), _faker.Internet.Password());

    public static string GenerateToken() => _faker.Random.AlphaNumeric(32);
}
