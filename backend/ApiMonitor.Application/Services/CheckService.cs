using System.Diagnostics;
using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Observability;
using ApiMonitor.Application.Interfaces;
using ApiMonitor.Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiMonitor.Application.Services;

public class CheckService(
    IEndpointRepository endpoints,
    ICheckResultRepository results,
    IEndpointChecker checker,
    IValidator<CheckHistoryQuery> historyValidator,
    IOptions<MonitoringOptions> options,
    MonitoringTelemetry telemetry,
    ILogger<CheckService> logger)
{
    /// <summary>
    /// Executa uma verificação completa: requisição, evento no MongoDB e status no PostgreSQL.
    /// Retorna null se o endpoint não existe mais (removido enquanto estava na fila).
    /// </summary>
    public async Task<CheckResultResponse?> RunCheckAsync(Guid endpointId, CancellationToken ct)
    {
        // Span raiz nas verificações do worker; filho do request HTTP na verificação manual.
        using var activity = MonitoringTelemetry.ActivitySource.StartActivity("monitoring.check");
        activity?.SetTag("endpoint.id", endpointId);

        var endpoint = await endpoints.GetAsync(endpointId, ct);
        if (endpoint is null) return null;
        activity?.SetTag("endpoint.name", endpoint.Name);

        // A URL fica fora dos logs: query strings de APIs de terceiros costumam carregar chaves.
        logger.LogDebug("Monitoring check started for {EndpointId} ({EndpointName})", endpoint.Id, endpoint.Name);

        var result = await checker.CheckAsync(endpoint, ct);
        telemetry.RecordCheck(result);
        activity?.SetTag("http.response.status_code", result.StatusCode);
        if (!result.Success)
            activity?.SetStatus(ActivityStatusCode.Error, result.ErrorType?.ToString()).SetTag("error.type", result.ErrorType?.ToString());

        await results.AddAsync(result, ct);

        var previousStatus = endpoint.LastStatus;
        var statusChanged = endpoint.RecordCheck(result);
        await endpoints.SaveChangesAsync(ct);

        if (result.Success)
            logger.LogInformation("Monitoring check completed for {EndpointId}: {StatusCode} in {LatencyMs} ms",
                endpoint.Id, result.StatusCode, result.LatencyMs);
        else
            logger.LogWarning("Monitoring check failed for {EndpointId}: {ErrorType} (status {StatusCode}, {LatencyMs} ms) {ErrorMessage}",
                endpoint.Id, result.ErrorType, result.StatusCode, result.LatencyMs, result.ErrorMessage);

        if (statusChanged)
            logger.LogInformation("Endpoint {EndpointId} status changed {PreviousStatus} -> {Status}",
                endpoint.Id, previousStatus, endpoint.LastStatus);

        // Dispara uma vez ao atingir o limite; volta a disparar só após uma recuperação.
        // ponytail: alerta só em log; e-mail/Slack/webhook entram atrás de uma interface quando existir o 2º canal.
        if (endpoint.ConsecutiveFailures == options.Value.AlertFailureThreshold)
            logger.LogError("ALERT: endpoint {EndpointId} ({EndpointName}) failed {Failures} consecutive checks",
                endpoint.Id, endpoint.Name, endpoint.ConsecutiveFailures);

        return CheckResultResponse.From(result);
    }

    public async Task<CheckResultResponse> CheckNowAsync(Guid endpointId, CancellationToken ct) =>
        await RunCheckAsync(endpointId, ct) ?? throw EndpointNotFound();

    public async Task<PagedResponse<CheckResultResponse>> GetHistoryAsync(Guid endpointId, CheckHistoryQuery query, CancellationToken ct)
    {
        await historyValidator.ValidateAndThrowAsync(query, ct);
        if (await endpoints.GetAsync(endpointId, ct) is null) throw EndpointNotFound();

        var (items, total) = await results.GetPageAsync(endpointId, query.From, query.To, query.Page, query.PageSize, ct);
        return new PagedResponse<CheckResultResponse>(
            items.Select(CheckResultResponse.From).ToList(), query.Page, query.PageSize, total);
    }

    private static NotFoundException EndpointNotFound() =>
        new("Endpoint not found", "The requested monitoring endpoint was not found.");
}
