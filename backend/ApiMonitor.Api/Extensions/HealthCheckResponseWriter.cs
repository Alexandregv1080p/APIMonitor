using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiMonitor.Api.Extensions;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Status geral e por dependência. Mensagens de exceção ficam só no log, não na resposta.</summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 1),
                    description = e.Value.Description
                })
        }, Options));
    }
}
