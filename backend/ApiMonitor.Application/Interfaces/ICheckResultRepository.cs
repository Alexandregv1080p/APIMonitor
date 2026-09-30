using ApiMonitor.Domain.Entities;

namespace ApiMonitor.Application.Interfaces;

public interface ICheckResultRepository
{
    Task AddAsync(CheckResult result, CancellationToken ct);

    Task<(IReadOnlyList<CheckResult> Items, long Total)> GetPageAsync(
        Guid endpointId, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct);

    Task DeleteByEndpointAsync(Guid endpointId, CancellationToken ct);
}
