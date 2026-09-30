using ApiMonitor.Application.Statistics;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Application.DTOs;

public record EndpointStatisticsResponse(
    string Period,
    double? UptimePercentage,
    double? AverageLatencyMs,
    long? MinLatencyMs,
    long? MaxLatencyMs,
    long TotalChecks,
    long SuccessfulChecks,
    long FailedChecks,
    IReadOnlyList<StatisticsPoint> Series)
{
    public static EndpointStatisticsResponse From(string period, CheckAggregate a, IReadOnlyList<StatisticsPoint> series) => new(
        period, a.UptimePercentage, a.AverageLatencyMs, a.MinLatencyMs, a.MaxLatencyMs,
        a.Total, a.Successful, a.Failed, series);
}

public record StatisticsPoint(
    DateTime BucketStart,
    long TotalChecks,
    long FailedChecks,
    double? UptimePercentage,
    double? AverageLatencyMs)
{
    public static StatisticsPoint From(DateTime bucketStart, CheckAggregate a) =>
        new(bucketStart, a.Total, a.Failed, a.UptimePercentage, a.AverageLatencyMs);
}

public record DashboardResponse(
    int TotalEndpoints,
    int UpEndpoints,
    int DownEndpoints,
    double? AverageLatencyMs,
    double? UptimePercentage,
    long FailedChecksLast24Hours,
    IReadOnlyList<DashboardEndpoint> Endpoints);

public record DashboardEndpoint(
    Guid Id,
    string Name,
    string Url,
    EndpointHttpMethod Method,
    bool Enabled,
    EndpointStatus Status,
    DateTime? LastCheckedAt,
    double? AverageLatencyMs,
    double? UptimePercentage);
