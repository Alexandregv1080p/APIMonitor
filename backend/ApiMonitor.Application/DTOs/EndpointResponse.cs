using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Application.DTOs;

public record EndpointResponse(
    Guid Id,
    string Name,
    string Url,
    EndpointHttpMethod Method,
    int IntervalSeconds,
    int TimeoutMilliseconds,
    int ExpectedStatusCode,
    bool Enabled,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? LastCheckedAt,
    EndpointStatus LastStatus)
{
    public static EndpointResponse From(MonitoredEndpoint e) => new(
        e.Id, e.Name, e.Url, e.Method, e.IntervalSeconds, e.TimeoutMilliseconds,
        e.ExpectedStatusCode, e.Enabled, e.CreatedAt, e.UpdatedAt, e.LastCheckedAt, e.LastStatus);
}
