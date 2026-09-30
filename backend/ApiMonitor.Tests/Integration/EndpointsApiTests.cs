using System.Net;
using System.Net.Http.Json;
using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace ApiMonitor.Tests.Integration;

[Collection(ApiCollection.Name)]
[Trait("Category", "Integration")]
public class EndpointsApiTests(ApiFactory factory) : ApiTestBase(factory.CreateClient())
{
    [Fact]
    public async Task Crud_lifecycle()
    {
        var created = await CreateAsync(Request("/ok"));
        Assert.Equal(EndpointStatus.Unknown, created.LastStatus);

        var fetched = await GetAsync<EndpointResponse>($"/api/endpoints/{created.Id}");
        Assert.Equal(created.Name, fetched.Name);

        var updated = await Client.PutAsJsonAsync($"/api/endpoints/{created.Id}",
            Request("/ok") with { Name = "Renamed", Method = EndpointHttpMethod.Head }, ApiFactory.Json);
        updated.EnsureSuccessStatusCode();
        Assert.Equal("Renamed", (await GetAsync<EndpointResponse>($"/api/endpoints/{created.Id}")).Name);

        var patched = await Client.PatchAsJsonAsync($"/api/endpoints/{created.Id}/status", new { enabled = true });
        patched.EnsureSuccessStatusCode();
        var enabledList = await GetAsync<List<EndpointResponse>>("/api/endpoints?enabled=true");
        Assert.Contains(enabledList, e => e.Id == created.Id && e.Method == EndpointHttpMethod.Head);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/api/endpoints/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Client.GetAsync($"/api/endpoints/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Invalid_request_returns_validation_problem_details()
    {
        var response = await Client.PostAsJsonAsync("/api/endpoints",
            Request("/ok") with { Url = "ftp://nope", IntervalSeconds = 1 }, ApiFactory.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("Url", problem!.Errors.Keys);
        Assert.Contains("IntervalSeconds", problem.Errors.Keys);
    }

    [Fact]
    public async Task Unknown_endpoint_returns_not_found_problem_details()
    {
        var response = await Client.PostAsync($"/api/endpoints/{Guid.NewGuid()}/check", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Endpoint not found", problem!.Title);
    }

    [Fact]
    public async Task Successful_check_is_persisted_and_marks_endpoint_up()
    {
        var endpoint = await CreateAsync(Request("/ok"));

        var result = await CheckNowAsync(endpoint.Id);

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(4, result.ResponseSizeBytes);

        var after = await GetAsync<EndpointResponse>($"/api/endpoints/{endpoint.Id}");
        Assert.Equal(EndpointStatus.Up, after.LastStatus);
        Assert.NotNull(after.LastCheckedAt);

        var history = await GetAsync<PagedResponse<CheckResultResponse>>($"/api/endpoints/{endpoint.Id}/checks");
        Assert.Equal(1, history.Total);

        var stats = await GetAsync<EndpointStatisticsResponse>($"/api/endpoints/{endpoint.Id}/statistics?period=24h");
        Assert.Equal(1, stats.TotalChecks);
        Assert.Equal(100, stats.UptimePercentage);
        Assert.Single(stats.Series);

        var dashboard = await GetAsync<DashboardResponse>("/api/dashboard");
        Assert.Contains(dashboard.Endpoints, e => e.Id == endpoint.Id && e.Status == EndpointStatus.Up && e.UptimePercentage == 100);
    }

    [Theory]
    [InlineData("/error", CheckErrorType.UnexpectedStatusCode, 500)]
    [InlineData("/slow", CheckErrorType.Timeout, null)]
    [InlineData("/refused", CheckErrorType.ConnectionError, null)]
    public async Task Failed_check_is_classified_and_marks_endpoint_down(string path, CheckErrorType expectedError, int? expectedStatus)
    {
        var endpoint = await CreateAsync(Request(path, timeoutMs: 200));

        var result = await CheckNowAsync(endpoint.Id);

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.ErrorType);
        Assert.Equal(expectedStatus, result.StatusCode);
        Assert.Equal(EndpointStatus.Down, (await GetAsync<EndpointResponse>($"/api/endpoints/{endpoint.Id}")).LastStatus);

        var stats = await GetAsync<EndpointStatisticsResponse>($"/api/endpoints/{endpoint.Id}/statistics");
        Assert.Equal(0, stats.UptimePercentage);
        Assert.Equal(1, stats.FailedChecks);
    }

    [Fact]
    public async Task History_is_paginated_newest_first()
    {
        var endpoint = await CreateAsync(Request("/ok"));
        for (var i = 0; i < 3; i++) await CheckNowAsync(endpoint.Id);

        var page1 = await GetAsync<PagedResponse<CheckResultResponse>>($"/api/endpoints/{endpoint.Id}/checks?pageSize=2");
        var page2 = await GetAsync<PagedResponse<CheckResultResponse>>($"/api/endpoints/{endpoint.Id}/checks?pageSize=2&page=2");

        Assert.Equal(3, page1.Total);
        Assert.Equal(2, page1.Items.Count);
        Assert.Single(page2.Items);
        Assert.True(page1.Items[0].CheckedAt >= page1.Items[1].CheckedAt);
        Assert.True(page1.Items[1].CheckedAt >= page2.Items[0].CheckedAt);
    }

    [Fact]
    public async Task Deleting_endpoint_removes_its_history()
    {
        var endpoint = await CreateAsync(Request("/ok"));
        await CheckNowAsync(endpoint.Id);

        (await Client.DeleteAsync($"/api/endpoints/{endpoint.Id}")).EnsureSuccessStatusCode();

        using var scope = factory.Services.CreateScope();
        var results = scope.ServiceProvider.GetRequiredService<ICheckResultRepository>();
        var (_, total) = await results.GetPageAsync(endpoint.Id, null, null, 1, 10, CancellationToken.None);
        Assert.Equal(0, total);
    }
}
