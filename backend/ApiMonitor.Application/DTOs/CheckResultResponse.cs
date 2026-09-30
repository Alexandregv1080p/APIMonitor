using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Application.DTOs;

public record CheckResultResponse(
    DateTime CheckedAt,
    bool Success,
    int? StatusCode,
    long LatencyMs,
    long? ResponseSizeBytes,
    CheckErrorType? ErrorType,
    string? ErrorMessage)
{
    public static CheckResultResponse From(CheckResult r) => new(
        r.CheckedAt, r.Success, r.StatusCode, r.LatencyMs, r.ResponseSizeBytes, r.ErrorType, r.ErrorMessage);
}

public record CheckHistoryQuery(DateTime? From = null, DateTime? To = null, int Page = 1, int PageSize = 50);

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);
