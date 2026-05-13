# API Automation Framework

REST API test automation framework built on .NET 10 using xUnit, RestSharp, and Allure for reporting.

## Project Structure

```
├── src/
│   └── ApiAutomation.Core/          # Core library (API client, models)
│       ├── PlayerApiClient.cs        # HTTP client for Player API (RestSharp)
│       ├── ApiException.cs           # Custom exception with HTTP status code
│       ├── RequestLogEntry.cs        # HTTP request/response log model
│       └── Models/
│           ├── Player.cs             # Player model
│           ├── PlayerResponse.cs     # Create player response
│           ├── LoginRequest.cs       # Login request body
│           └── LoginResponse.cs      # Login response with token
├── tests/
│   └── ApiAutomation.Tests/          # Test project
│       ├── Tests/
│       │   └── EndToEndTests.cs      # E2E tests (login, CRUD players)
│       ├── Services/
│       │   └── PlayerApiService.cs   # Test state preparation service
│       ├── TestData/
│       │   └── PlayerFaker.cs        # Test data generation (Bogus)
│       └── FakeHttpHandler.cs        # Mock HTTP handler for isolated tests
├── .gitlab-ci.yml                    # CI pipeline (test → report → pages)
└── ApiAutomation.sln                 # Solution file
```

## Tech Stack

| Component | Library | Purpose |
|-----------|---------|---------|
| HTTP client | RestSharp | API request execution |
| Test framework | xUnit | Test runner |
| Assertions | FluentAssertions | Readable assertions |
| Test data | Bogus | Fake data generation |
| Property-based testing | FsCheck.Xunit | Invariant verification |
| Reporting | Allure.Xunit | Allure reports |
| CI | GitLab CI + Docker | Automated execution |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

## Running Tests

### Locally

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build --no-restore

# Run all tests
dotnet test

# Run with verbose output
dotnet test --verbosity normal

# Run a specific test
dotnet test --filter "Login_ReturnsValidToken"
```

### Allure Report (locally)

```bash
# Run tests (results go to allure-results/)
dotnet test

# Generate report (requires allure CLI)
allure generate allure-results -o allure-report --clean
allure open allure-report
```

## CI/CD

The GitLab CI pipeline consists of three stages:

1. **test** — build and run tests inside `mcr.microsoft.com/dotnet/sdk:10.0` Docker container
2. **report** — generate Allure report
3. **pages** — publish report to GitLab Pages (`main` branch only)

Tests run automatically on every push.

## Adding a New Test

1. Create a method in `EndToEndTests.cs` (or a new class in `Tests/`)
2. Set up mock responses via `PlayerApiService` / `FakeHttpHandler`
3. Use `PlayerFaker` to generate test data
4. Add Allure steps with `AllureApi.Step()`

### Example

```csharp
[Fact]
public async Task MyNewTest()
{
    _api.Login(_authToken);
    var player = PlayerFaker.GenerateNewPlayer();

    AllureApi.Step("Create player");
    var response = await _client.CreatePlayerAsync(player, _authToken);

    AllureApi.Step("Verify response");
    response.Player.Id.Should().NotBeNullOrEmpty();
}
```

## How FakeHttpHandler Works

Tests run without a real server. `FakeHttpHandler` replaces the HTTP transport layer and returns pre-configured responses. This allows:

- Running tests offline
- Testing client logic in isolation
- Having predictable and fast tests

To switch to a real server — remove `FakeHttpHandler` from the `PlayerApiClient` constructor and pass a real base URL.

## Updating Dependencies

```bash
# Check for available updates
dotnet list package --outdated

# Update a specific package
dotnet add tests/ApiAutomation.Tests package FluentAssertions
```

When upgrading the .NET SDK — update `TargetFramework` in `.csproj` files and the image tag in `.gitlab-ci.yml`.
