using ApiMonitor.Application.Observability;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ApiMonitor.Api.Extensions;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Métricas sempre ligadas (expostas em /metrics para o Prometheus).
    /// Traces só quando há um coletor OTLP configurado (OTEL_EXPORTER_OTLP_ENDPOINT), para não gerar spans à toa.
    /// </summary>
    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var otel = services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService("api-monitor"))
            .WithMetrics(m => m
                .AddMeter(MonitoringTelemetry.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        if (!string.IsNullOrWhiteSpace(configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            otel.WithTracing(t => t
                .AddSource(MonitoringTelemetry.Name)
                .AddAspNetCoreInstrumentation(o => o.Filter = ctx =>
                    !ctx.Request.Path.StartsWithSegments("/health") && !ctx.Request.Path.StartsWithSegments("/metrics"))
                .AddHttpClientInstrumentation() // query strings já saem redigidas por padrão
                .AddNpgsql()
                .AddOtlpExporter());
        }

        return services;
    }
}
