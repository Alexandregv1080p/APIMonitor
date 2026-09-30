using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Application.Statistics;
using ApiMonitor.Domain.Enums;
using ApiMonitor.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace ApiMonitor.Application.Services;

public class StatisticsService(IEndpointRepository endpoints, ICheckResultRepository results)
{
    public async Task<EndpointStatisticsResponse> GetEndpointStatisticsAsync(Guid endpointId, string? period, CancellationToken ct)
    {
        var p = StatisticsPeriod.Parse(period) ?? throw new ValidationException(
            [new ValidationFailure("period", $"'period' must be one of: {string.Join(", ", StatisticsPeriod.All.Select(x => x.Name))}.")]);

        if (await endpoints.GetAsync(endpointId, ct) is null)
            throw new NotFoundException("Endpoint not found", "The requested monitoring endpoint was not found.");

        var since = DateTime.UtcNow - p.Window;
        var totalTask = results.AggregateAsync(endpointId, since, ct);
        var series = await results.AggregateSeriesAsync(endpointId, since, p.Bucket, ct);

        return EndpointStatisticsResponse.From(p.Name, await totalTask,
            series.Select(s => StatisticsPoint.From(s.BucketStart, s.Aggregate)).ToList());
    }

    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct)
    {
        var all = await endpoints.ListAsync(status: null, enabled: null, ct);
        var byEndpoint = await results.AggregateByEndpointAsync(DateTime.UtcNow - StatisticsPeriod.Last24Hours.Window, ct);

        var rows = all.Select(e =>
        {
            var a = byEndpoint.GetValueOrDefault(e.Id, CheckAggregate.Empty);
            return new DashboardEndpoint(e.Id, e.Name, e.Url, e.Method, e.Enabled, e.LastStatus, e.LastCheckedAt,
                a.AverageLatencyMs, a.UptimePercentage);
        }).ToList();

        // Só conta resultados de endpoints que ainda existem.
        var overall = all.Aggregate(CheckAggregate.Empty,
            (acc, e) => acc.Combine(byEndpoint.GetValueOrDefault(e.Id, CheckAggregate.Empty)));

        return new DashboardResponse(
            all.Count,
            all.Count(e => e.LastStatus == EndpointStatus.Up),
            all.Count(e => e.LastStatus == EndpointStatus.Down),
            overall.AverageLatencyMs,
            overall.UptimePercentage,
            overall.Failed,
            rows);
    }
}
