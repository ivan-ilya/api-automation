using System.Net;

namespace ApiAutomation.Tests;

public class FakeHttpHandler : HttpMessageHandler
{
    private readonly List<FakeResponse> _responses = new();

    public void SetupResponse(string path, HttpMethod method, HttpStatusCode statusCode, string responseBody)
    {
        _responses.Add(new FakeResponse
        {
            Path = path,
            Method = method,
            StatusCode = statusCode,
            ResponseBody = responseBody
        });
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;

        var match = _responses.FirstOrDefault(r =>
            requestPath.Equals(r.Path, StringComparison.OrdinalIgnoreCase) &&
            request.Method == r.Method);

        if (match != null)
        {
            var response = new HttpResponseMessage(match.StatusCode)
            {
                Content = new StringContent(match.ResponseBody, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }

        var errorMessage = $"No response configured for {request.Method} {requestPath}";
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent(errorMessage, System.Text.Encoding.UTF8, "text/plain")
        };
        return Task.FromResult(errorResponse);
    }

    private class FakeResponse
    {
        public string Path { get; set; } = string.Empty;
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public HttpStatusCode StatusCode { get; set; }
        public string ResponseBody { get; set; } = string.Empty;
    }
}
