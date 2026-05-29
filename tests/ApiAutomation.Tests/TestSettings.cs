using Microsoft.Extensions.Configuration;

namespace ApiAutomation.Tests;

/// <summary>
/// Reads test configuration from appsettings.test.json and environment variables.
/// Environment variables use the "Api__" prefix (e.g., Api__BaseUrl, Api__UseMocks).
/// </summary>
public class TestSettings
{
    public string BaseUrl { get; set; } = "http://localhost";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseMocks { get; set; } = true;

    public static TestSettings Load()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.test.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var section = configuration.GetSection("Api");
        var settings = new TestSettings
        {
            BaseUrl = section["BaseUrl"] ?? "http://localhost",
            Username = section["Username"] ?? string.Empty,
            Password = section["Password"] ?? string.Empty,
            UseMocks = bool.TryParse(section["UseMocks"], out var useMocks) ? useMocks : true
        };
        return settings;
    }
}
