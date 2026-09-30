using System.Collections.Concurrent;
using ApiMonitor.Application;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiMonitor.Infrastructure.Monitoring;

/// <summary>
/// A cada tick busca endpoints vencidos e dispara as verificações sem esperar por elas,
/// limitadas por um semáforo. Assim uma API lenta ocupa só a própria vaga e não atrasa as demais.
/// </summary>
// ponytail: estado de "em andamento" é local ao processo. Com várias instâncias do worker,
// um mesmo endpoint pode ser verificado em duplicidade; resolver com fila ou lock distribuído.
public class MonitoringWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> options,
    ILogger<MonitoringWorker> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<Guid, byte> _inFlight = new();
    private readonly SemaphoreSlim _slots = new(options.Value.MaxConcurrentChecks);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        if (!opts.WorkerEnabled)
        {
            logger.LogInformation("Monitoring worker disabled by configuration");
            return;
        }

        logger.LogInformation("Monitoring worker started (max {MaxConcurrentChecks} concurrent checks, polling every {PollIntervalSeconds}s)",
            opts.MaxConcurrentChecks, opts.PollIntervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(opts.PollIntervalSeconds));
        try
        {
            do
            {
                await DispatchDueChecksAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        logger.LogInformation("Monitoring worker stopped");
    }

    private async Task DispatchDueChecksAsync(CancellationToken ct)
    {
        IReadOnlyList<Guid> due;
        try
        {
            using var scope = scopeFactory.CreateScope();
            due = await scope.ServiceProvider.GetRequiredService<IEndpointRepository>().ListDueIdsAsync(DateTime.UtcNow, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to load endpoints due for checking");
            return;
        }

        foreach (var id in due)
        {
            if (!_inFlight.TryAdd(id, 0)) continue; // verificação anterior ainda em andamento

            await _slots.WaitAsync(ct);
            _ = RunCheckAsync(id, ct);
        }
    }

    private async Task RunCheckAsync(Guid endpointId, CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<CheckService>().RunCheckAsync(endpointId, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Monitoring check crashed for {EndpointId}", endpointId);
        }
        finally
        {
            _inFlight.TryRemove(endpointId, out _);
            _slots.Release();
        }
    }
}
