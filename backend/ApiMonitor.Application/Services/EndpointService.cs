using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;
using ApiMonitor.Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace ApiMonitor.Application.Services;

public class EndpointService(
    IEndpointRepository repository,
    ICheckResultRepository checkResults,
    IValidator<EndpointRequest> validator,
    ILogger<EndpointService> logger)
{
    public async Task<IReadOnlyList<EndpointResponse>> ListAsync(EndpointStatus? status, bool? enabled, CancellationToken ct)
    {
        var endpoints = await repository.ListAsync(status, enabled, ct);
        return endpoints.Select(EndpointResponse.From).ToList();
    }

    public async Task<EndpointResponse> GetAsync(Guid id, CancellationToken ct) =>
        EndpointResponse.From(await FindAsync(id, ct));

    public async Task<EndpointResponse> CreateAsync(EndpointRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);

        var endpoint = new MonitoredEndpoint(
            request.Name, request.Url, request.Method, request.IntervalSeconds,
            request.TimeoutMilliseconds, request.ExpectedStatusCode, request.Enabled);

        repository.Add(endpoint);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Endpoint {EndpointId} created ({EndpointName})", endpoint.Id, endpoint.Name);
        return EndpointResponse.From(endpoint);
    }

    public async Task<EndpointResponse> UpdateAsync(Guid id, EndpointRequest request, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        var endpoint = await FindAsync(id, ct);

        endpoint.Update(
            request.Name, request.Url, request.Method, request.IntervalSeconds,
            request.TimeoutMilliseconds, request.ExpectedStatusCode, request.Enabled);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Endpoint {EndpointId} updated", endpoint.Id);
        return EndpointResponse.From(endpoint);
    }

    public async Task<EndpointResponse> SetEnabledAsync(Guid id, bool enabled, CancellationToken ct)
    {
        var endpoint = await FindAsync(id, ct);
        endpoint.SetEnabled(enabled);
        await repository.SaveChangesAsync(ct);

        logger.LogInformation("Endpoint {EndpointId} monitoring {State}", endpoint.Id, enabled ? "enabled" : "disabled");
        return EndpointResponse.From(endpoint);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var endpoint = await FindAsync(id, ct);
        repository.Remove(endpoint);
        await repository.SaveChangesAsync(ct);
        await checkResults.DeleteByEndpointAsync(id, ct);

        logger.LogInformation("Endpoint {EndpointId} deleted", id);
    }

    private async Task<MonitoredEndpoint> FindAsync(Guid id, CancellationToken ct) =>
        await repository.GetAsync(id, ct)
        ?? throw new NotFoundException("Endpoint not found", "The requested monitoring endpoint was not found.");
}
