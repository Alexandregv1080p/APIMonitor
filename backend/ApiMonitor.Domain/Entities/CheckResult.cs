using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Domain.Entities;

/// <summary>Resultado imutável de uma verificação. Persistido como evento no MongoDB.</summary>
public record CheckResult(
    Guid EndpointId,
    DateTime CheckedAt,
    bool Success,
    int? StatusCode,
    long LatencyMs,
    long? ResponseSizeBytes,
    CheckErrorType? ErrorType,
    string? ErrorMessage);
