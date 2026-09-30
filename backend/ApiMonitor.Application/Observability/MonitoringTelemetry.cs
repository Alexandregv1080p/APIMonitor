using System.Diagnostics;
using System.Diagnostics.Metrics;
using ApiMonitor.Domain.Entities;

namespace ApiMonitor.Application.Observability;

/// <summary>
/// Métricas e traces do monitoramento com as APIs nativas do .NET (System.Diagnostics).
/// O OpenTelemetry só coleta e exporta; este código não depende dele.
/// No Prometheus: monitor_checks_total, monitor_checks_failed_total{error_type}, monitor_check_latency_milliseconds,
/// monitor_http_status_total{status_code}, monitor_endpoints_up/down, monitor_worker_duration_milliseconds.
/// </summary>
public sealed class MonitoringTelemetry
{
    public const string Name = "ApiMonitor";
    public static readonly ActivitySource ActivitySource = new(Name);

    private readonly Counter<long> _checks;
    private readonly Counter<long> _failedChecks;
    private readonly Counter<long> _httpStatus;
    private readonly Histogram<double> _latency;
    private readonly Histogram<double> _workerDuration;
    private int _endpointsUp;
    private int _endpointsDown;

    public MonitoringTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Name);
        _checks = meter.CreateCounter<long>("monitor.checks", description: "Verificações executadas.");
        _failedChecks = meter.CreateCounter<long>("monitor.checks.failed", description: "Verificações com falha, por tipo de erro.");
        _httpStatus = meter.CreateCounter<long>("monitor.http.status", description: "Códigos HTTP recebidos dos endpoints monitorados.");
        _latency = meter.CreateHistogram<double>("monitor.check.latency", unit: "ms", description: "Latência das verificações com resposta HTTP.");
        _workerDuration = meter.CreateHistogram<double>("monitor.worker.duration", unit: "ms", description: "Duração de cada ciclo de agendamento do worker.");
        meter.CreateObservableGauge("monitor.endpoints.up", () => Volatile.Read(ref _endpointsUp), description: "Endpoints ativos com status UP.");
        meter.CreateObservableGauge("monitor.endpoints.down", () => Volatile.Read(ref _endpointsDown), description: "Endpoints ativos com status DOWN.");
    }

    public void RecordCheck(CheckResult result)
    {
        _checks.Add(1, new KeyValuePair<string, object?>("outcome", result.Success ? "success" : "failure"));

        if (!result.Success)
            _failedChecks.Add(1, new KeyValuePair<string, object?>("error_type", result.ErrorType?.ToString() ?? "Unknown"));

        if (result.StatusCode is { } status)
        {
            _httpStatus.Add(1, new KeyValuePair<string, object?>("status_code", status));
            _latency.Record(result.LatencyMs);
        }
    }

    public void RecordWorkerCycle(TimeSpan duration) => _workerDuration.Record(duration.TotalMilliseconds);

    public void SetEndpointCounts(int up, int down)
    {
        Volatile.Write(ref _endpointsUp, up);
        Volatile.Write(ref _endpointsDown, down);
    }
}
