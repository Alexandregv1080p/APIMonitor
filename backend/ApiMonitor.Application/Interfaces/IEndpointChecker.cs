using ApiMonitor.Domain.Entities;

namespace ApiMonitor.Application.Interfaces;

/// <summary>Executa a requisição de verificação. Nunca lança para falhas do alvo (timeout, rede, status): elas viram CheckResult.</summary>
public interface IEndpointChecker
{
    Task<CheckResult> CheckAsync(MonitoredEndpoint endpoint, CancellationToken ct);
}
