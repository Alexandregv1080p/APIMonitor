using System.Net;
using System.Text.Json;

namespace ApiMonitor.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class HealthTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Liveness_is_healthy_without_checking_dependencies()
    {
        var response = await _client.GetAsync("/health");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Empty(body.RootElement.GetProperty("checks").EnumerateObject());
    }

    [Fact]
    public async Task Readiness_checks_postgres_and_mongo()
    {
        var response = await _client.GetAsync("/health/ready");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var checks = body.RootElement.GetProperty("checks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", checks.GetProperty("postgresql").GetProperty("status").GetString());
        Assert.Equal("Healthy", checks.GetProperty("mongodb").GetProperty("status").GetString());
    }
}
