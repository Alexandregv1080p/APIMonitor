using System.Net;
using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;
using ApiMonitor.Infrastructure.Monitoring;

namespace ApiMonitor.Tests.Monitoring;

public class HttpEndpointCheckerTests
{
    private static MonitoredEndpoint Endpoint(int timeoutMs = 1000, int expected = 200) =>
        new("Test", "http://monitored.test/health", EndpointHttpMethod.Get, 60, timeoutMs, expected, true);

    private static HttpEndpointChecker Checker(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new FakeHttpClientFactory(new StubHandler(handler)));

    [Fact]
    public async Task Expected_status_is_success_with_size_and_latency()
    {
        var checker = Checker((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("hello")
        }));

        var result = await checker.CheckAsync(Endpoint(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(5, result.ResponseSizeBytes);
        Assert.Null(result.ErrorType);
        Assert.True(result.LatencyMs >= 0);
    }

    [Fact]
    public async Task Unexpected_status_is_failure_with_status_code()
    {
        var checker = Checker((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

        var result = await checker.CheckAsync(Endpoint(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(500, result.StatusCode);
        Assert.Equal(CheckErrorType.UnexpectedStatusCode, result.ErrorType);
    }

    [Fact]
    public async Task Custom_expected_status_is_respected()
    {
        var checker = Checker((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent)));

        var result = await checker.CheckAsync(Endpoint(expected: 204), CancellationToken.None);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Slow_response_becomes_timeout()
    {
        var checker = Checker(async (_, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var result = await checker.CheckAsync(Endpoint(timeoutMs: 100), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Null(result.StatusCode);
        Assert.Equal(CheckErrorType.Timeout, result.ErrorType);
        Assert.InRange(result.LatencyMs, 90, 5000);
    }

    [Fact]
    public async Task Connection_error_is_reported_not_thrown()
    {
        var checker = Checker((_, _) => throw new HttpRequestException("Connection refused"));

        var result = await checker.CheckAsync(Endpoint(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(CheckErrorType.ConnectionError, result.ErrorType);
        Assert.Equal("Connection refused", result.ErrorMessage);
    }

    [Fact]
    public async Task Shutdown_cancellation_propagates_instead_of_becoming_timeout()
    {
        using var cts = new CancellationTokenSource();
        var checker = Checker(async (_, ct) =>
        {
            cts.Cancel();
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => checker.CheckAsync(Endpoint(), cts.Token));
    }

    private sealed class StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            handler(request, ct);
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}
