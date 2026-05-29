# API Automation Framework

REST API test automation framework built on .NET 10 using xUnit, RestSharp, and Allure for reporting.

> **Note on .NET version:** This project targets .NET 10 (current LTS, supported until November 2028).
> The framework is fully compatible with .NET 6/8 — simply change `TargetFramework` in `.csproj` files
> and the container image tag in the CI workflow to target the version approved by your team.

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
│       │   ├── EndToEndTests.cs      # E2E tests (login, CRUD players)
│       │   └── NegativeTests.cs      # Negative scenarios (401, 403, 404)
│       ├── Services/
│       │   └── PlayerApiService.cs   # Test state preparation service
│       ├── TestData/
│       │   └── PlayerFaker.cs        # Test data generation (Bogus)
│       ├── FakeHttpHandler.cs        # Mock HTTP handler for isolated tests
│       └── appsettings.test.json     # Test configuration (URL, credentials, mock toggle)
├── .github/
│   └── workflows/
│       └── test.yml                  # GitHub Actions CI pipeline
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
| Configuration | Microsoft.Extensions.Configuration | Environment-based settings |
| CI | GitHub Actions | Automated execution & Allure Pages |

## Configuration

Tests are configured via `appsettings.test.json` and environment variables:

```json
{
  "Api": {
    "BaseUrl": "https://api.example.com",
    "Username": "tester",
    "Password": "secret",
    "UseMocks": true
  }
}
```

| Key | Description | Default |
|-----|-------------|---------|
| `Api:BaseUrl` | Target API base URL | `http://localhost` |
| `Api:Username` | Login username | — |
| `Api:Password` | Login password | — |
| `Api:UseMocks` | Use FakeHttpHandler instead of real HTTP | `true` |

Environment variables override JSON config using the standard `Api__BaseUrl` convention.

When `UseMocks=true` (default), tests run offline using `FakeHttpHandler`.
Set `UseMocks=false` and provide real credentials to run against a live server.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or .NET 8 — see note above)

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

# Run against real API
Api__BaseUrl=https://real-api.example.com Api__UseMocks=false dotnet test
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

The GitHub Actions pipeline (`.github/workflows/test.yml`) consists of three jobs:

1. **test** — build and run tests inside `mcr.microsoft.com/dotnet/sdk:10.0` container
2. **allure-report** — generate Allure report from test results
3. **pages** — publish report to GitHub Pages (`main` branch only)

Tests run automatically on every push and pull request.

## Test Strategy

### Positive Tests (EndToEndTests)
- Login → valid token
- Create 12 players → each returns 201 with valid ID
- Get player by ID → correct data
- Get all players sorted → alphabetical order
- Delete players → success

### Negative Tests (NegativeTests)
- Login with invalid credentials → 401 Unauthorized
- Create player without auth token → 401 Unauthorized
- Get player with unknown ID → 404 Not Found
- Delete already-deleted player → 404 Not Found

### Schema Validation
Response models are strongly typed (`Player`, `PlayerResponse`, `LoginResponse`).
Tests validate:
- Required fields are present and non-null
- Field types match expected schema (e.g., `Id` is a valid GUID string)
- Response structure matches API documentation

### Cleanup Strategy

In mock mode, each test is fully isolated — `FakeHttpHandler` provides deterministic responses
and no shared state exists between tests.

In a real API scenario, the framework supports a cleanup pattern:
- Created resource IDs are tracked in a `List<string>` within the test class
- `Dispose()` deletes all tracked resources in a finally/teardown block
- This ensures no orphaned test data remains after test execution
- Tests use `IDisposable` to guarantee cleanup even on failure

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

To switch to a real server — set `UseMocks=false` in `appsettings.test.json` or via environment variable.

## Updating Dependencies

```bash
# Check for available updates
dotnet list package --outdated

# Update a specific package
dotnet add tests/ApiAutomation.Tests package FluentAssertions
```

When upgrading the .NET SDK — update `TargetFramework` in `.csproj` files and the container image tag in `.github/workflows/test.yml`.
