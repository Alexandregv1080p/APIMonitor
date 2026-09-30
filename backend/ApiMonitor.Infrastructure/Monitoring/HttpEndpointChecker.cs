using System.Diagnostics;
using System.Net.Sockets;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Infrastructure.Monitoring;

public class HttpEndpointChecker(IHttpClientFactory httpClientFactory) : IEndpointChecker
{
    public const string ClientName = "MonitorClient";

    /// <summary>Limite de leitura do corpo: mede o tamanho sem baixar respostas gigantes inteiras.</summary>
    public const int MaxBodyBytes = 10 * 1024 * 1024;

    public async Task<CheckResult> CheckAsync(MonitoredEndpoint endpoint, CancellationToken ct)
    {
        var checkedAt = DateTime.UtcNow;
        var client = httpClientFactory.CreateClient(ClientName);

        // O timeout vale para a verificação inteira (headers + corpo), por endpoint.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(endpoint.TimeoutMilliseconds);

        using var request = new HttpRequestMessage(new HttpMethod(endpoint.Method.ToString().ToUpperInvariant()), endpoint.Url);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token);
            var latencyMs = stopwatch.ElapsedMilliseconds; // tempo até os headers
            var size = await MeasureBodyAsync(response, timeoutCts.Token);

            var statusCode = (int)response.StatusCode;
            var success = statusCode == endpoint.ExpectedStatusCode;
            return new CheckResult(endpoint.Id, checkedAt, success, statusCode, latencyMs, size,
                success ? null : CheckErrorType.UnexpectedStatusCode,
                success ? null : $"Expected HTTP {endpoint.ExpectedStatusCode} but received {statusCode}.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Failure(CheckErrorType.Timeout, $"The request timed out after {endpoint.TimeoutMilliseconds} ms.");
        }
        catch (HttpRequestException ex)
        {
            var type = ex.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData }
                ? CheckErrorType.DnsError
                : CheckErrorType.ConnectionError;
            return Failure(type, ex.Message);
        }

        CheckResult Failure(CheckErrorType type, string message) =>
            new(endpoint.Id, checkedAt, false, null, stopwatch.ElapsedMilliseconds, null, type, message);
    }

    private static async Task<long> MeasureBodyAsync(HttpResponseMessage response, CancellationToken ct)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while (total < MaxBodyBytes && (read = await stream.ReadAsync(buffer, ct)) > 0)
            total += read;
        return total;
    }
}
