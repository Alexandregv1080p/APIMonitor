using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;

namespace ApiMonitor.Application.Interfaces;

public interface IEndpointRepository
{
    Task<IReadOnlyList<MonitoredEndpoint>> ListAsync(EndpointStatus? status, bool? enabled, CancellationToken ct);
    Task<IReadOnlyList<Guid>> ListDueIdsAsync(DateTime now, CancellationToken ct);
    Task<(int Up, int Down)> CountEnabledByStatusAsync(CancellationToken ct);
    Task<MonitoredEndpoint?> GetAsync(Guid id, CancellationToken ct);
    void Add(MonitoredEndpoint endpoint);
    void Remove(MonitoredEndpoint endpoint);
    Task SaveChangesAsync(CancellationToken ct);
}
