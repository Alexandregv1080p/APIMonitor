using ApiMonitor.Application.Statistics;
using ApiMonitor.Domain.Entities;

namespace ApiMonitor.Application.Interfaces;

public interface ICheckResultRepository
{
    Task AddAsync(CheckResult result, CancellationToken ct);

    Task<(IReadOnlyList<CheckResult> Items, long Total)> GetPageAsync(
        Guid endpointId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);

    Task DeleteByEndpointAsync(Guid endpointId, CancellationToken ct);

    Task<CheckAggregate> AggregateAsync(Guid endpointId, DateTime since, CancellationToken ct);

    Task<IReadOnlyList<(DateTime BucketStart, CheckAggregate Aggregate)>> AggregateSeriesAsync(
        Guid endpointId, DateTime since, TimeSpan bucket, CancellationToken ct);

    Task<IReadOnlyDictionary<Guid, CheckAggregate>> AggregateByEndpointAsync(DateTime since, CancellationToken ct);
}
