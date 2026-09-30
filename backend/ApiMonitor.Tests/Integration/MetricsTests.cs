namespace ApiMonitor.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class MetricsTests(ApiFactory factory) : ApiTestBase(factory.CreateClient())
{
    [Fact]
    public async Task Checks_are_exported_as_prometheus_metrics()
    {
        await CheckNowAsync((await CreateAsync(Request("/ok"))).Id);
        await CheckNowAsync((await CreateAsync(Request("/slow", timeoutMs: 100))).Id);

        var metrics = await Client.GetStringAsync("/metrics");

        Assert.Contains("monitor_checks_total{", metrics);
        Assert.Contains("outcome=\"success\"", metrics);
        Assert.Contains("monitor_checks_failed_total{", metrics);
        Assert.Contains("error_type=\"Timeout\"", metrics);
        Assert.Contains("monitor_check_latency_milliseconds_bucket", metrics);
        Assert.Contains("monitor_http_status_total{", metrics);
        Assert.Contains("http_server_request_duration_seconds", metrics); // instrumentação do ASP.NET Core
    }
}
