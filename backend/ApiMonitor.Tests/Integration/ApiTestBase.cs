using System.Net.Http.Json;
using ApiMonitor.Application.DTOs;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Tests.Integration;

public abstract class ApiTestBase(HttpClient client)
{
    protected HttpClient Client { get; } = client;

    protected static EndpointRequest Request(string path, int timeoutMs = 1000, bool enabled = false) =>
        new($"Test {path} {Guid.NewGuid():N}", TargetHandler.BaseUrl + path, EndpointHttpMethod.Get,
            IntervalSeconds: 10, TimeoutMilliseconds: timeoutMs, ExpectedStatusCode: 200, Enabled: enabled);

    protected async Task<EndpointResponse> CreateAsync(EndpointRequest request)
    {
        var response = await Client.PostAsJsonAsync("/api/endpoints", request, ApiFactory.Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EndpointResponse>(ApiFactory.Json))!;
    }

    protected async Task<T> GetAsync<T>(string url)
    {
        var response = await Client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(ApiFactory.Json))!;
    }

    protected async Task<CheckResultResponse> CheckNowAsync(Guid id)
    {
        var response = await Client.PostAsync($"/api/endpoints/{id}/check", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckResultResponse>(ApiFactory.Json))!;
    }
}
