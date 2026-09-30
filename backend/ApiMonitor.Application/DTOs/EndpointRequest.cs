using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Application.DTOs;

/// <summary>Body de criação (POST) e de substituição completa (PUT).</summary>
public record EndpointRequest(
    string Name,
    string Url,
    EndpointHttpMethod Method = EndpointHttpMethod.Get,
    int IntervalSeconds = 60,
    int TimeoutMilliseconds = 5000,
    int ExpectedStatusCode = 200,
    bool Enabled = true);

public record SetEnabledRequest(bool Enabled);
