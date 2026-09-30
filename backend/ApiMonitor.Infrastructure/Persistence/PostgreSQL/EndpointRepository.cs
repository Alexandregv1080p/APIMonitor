using ApiMonitor.Application.Interfaces;
using ApiMonitor.Domain.Entities;
using ApiMonitor.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ApiMonitor.Infrastructure.Persistence.PostgreSQL;

public class EndpointRepository(AppDbContext db) : IEndpointRepository
{
    public async Task<IReadOnlyList<MonitoredEndpoint>> ListAsync(EndpointStatus? status, bool? enabled, CancellationToken ct)
    {
        var query = db.Endpoints.AsNoTracking();
        if (status is not null) query = query.Where(e => e.LastStatus == status);
        if (enabled is not null) query = query.Where(e => e.Enabled == enabled);
        return await query.OrderBy(e => e.Name).ToListAsync(ct);
    }

    // ponytail: carrega todos os habilitados e filtra em memória com a regra do domínio (IsDue).
    // Serve até alguns milhares de endpoints; acima disso, levar o filtro de vencimento para o SQL.
    public async Task<IReadOnlyList<Guid>> ListDueIdsAsync(DateTime now, CancellationToken ct)
    {
        var enabled = await db.Endpoints.AsNoTracking().Where(e => e.Enabled).ToListAsync(ct);
        return enabled.Where(e => e.IsDue(now)).Select(e => e.Id).ToList();
    }

    public Task<MonitoredEndpoint?> GetAsync(Guid id, CancellationToken ct) =>
        db.Endpoints.FirstOrDefaultAsync(e => e.Id == id, ct);

    public void Add(MonitoredEndpoint endpoint) => db.Endpoints.Add(endpoint);

    public void Remove(MonitoredEndpoint endpoint) => db.Endpoints.Remove(endpoint);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
