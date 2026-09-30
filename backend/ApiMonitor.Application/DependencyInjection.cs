using ApiMonitor.Application.Observability;
using ApiMonitor.Application.Services;
using ApiMonitor.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ApiMonitor.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Mensagens sempre em inglês, independente da cultura do servidor (o resto da API é em inglês).
        ValidatorOptions.Global.LanguageManager.Enabled = false;
        services.AddValidatorsFromAssemblyContaining<EndpointRequestValidator>();
        services.AddScoped<EndpointService>();
        services.AddScoped<CheckService>();
        services.AddScoped<StatisticsService>();
        services.AddSingleton<MonitoringTelemetry>();
        return services;
    }
}
