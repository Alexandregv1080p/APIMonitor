using ApiMonitor.Application.DTOs;
using ApiMonitor.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ApiMonitor.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public sealed class MonitoringWorkerTests : ApiTestBase, IDisposable
{
    private readonly WebApplicationFactory<Program> _host;

    // Mesmos containers da coleção, mas com o worker ligado (poll de 1 s).
    public MonitoringWorkerTests(ApiFactory factory)
        : this(factory.WithWebHostBuilder(b => b.UseSetting("Monitoring:WorkerEnabled", "true"))) { }

    private MonitoringWorkerTests(WebApplicationFactory<Program> host) : base(host.CreateClient()) => _host = host;

    public void Dispose() => _host.Dispose();

    [Fact]
    public async Task Worker_checks_enabled_endpoints_automatically_and_updates_status()
    {
        var healthy = await CreateAsync(Request("/ok", enabled: true));
        var broken = await CreateAsync(Request("/error", enabled: true));
        var paused = await CreateAsync(Request("/ok", enabled: false));

        var h = await WaitForFirstCheckAsync(healthy.Id);
        var b = await WaitForFirstCheckAsync(broken.Id);

        Assert.Equal(EndpointStatus.Up, h.LastStatus);
        Assert.Equal(EndpointStatus.Down, b.LastStatus);

        var history = await GetAsync<PagedResponse<CheckResultResponse>>($"/api/endpoints/{broken.Id}/checks");
        Assert.True(history.Total >= 1);
        Assert.Equal(CheckErrorType.UnexpectedStatusCode, history.Items[0].ErrorType);

        // Endpoint pausado nunca é verificado pelo worker.
        Assert.Equal(EndpointStatus.Unknown, (await GetAsync<EndpointResponse>($"/api/endpoints/{paused.Id}")).LastStatus);
    }

    private async Task<EndpointResponse> WaitForFirstCheckAsync(Guid id)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (true)
        {
            var e = await GetAsync<EndpointResponse>($"/api/endpoints/{id}");
            if (e.LastStatus != EndpointStatus.Unknown || DateTime.UtcNow > deadline) return e;
            await Task.Delay(250);
        }
    }
}
