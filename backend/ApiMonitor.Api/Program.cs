using System.Text.Json;
using System.Text.Json.Serialization;
using ApiMonitor.Api.Extensions;
using ApiMonitor.Api.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ApiMonitor.Application;
using ApiMonitor.Infrastructure.DependencyInjection;
using ApiMonitor.Infrastructure.Persistence.PostgreSQL;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    // Enums como "UP", "DOWN", "GET"... tanto na entrada quanto na saída.
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper)));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(allowedOrigins)
    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
    .WithHeaders("Content-Type")));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseCors();
app.MapControllers();

// Liveness: o processo responde (sem dependências, para não reiniciar a API quando um banco oscila).
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
// Readiness: PostgreSQL e MongoDB acessíveis.
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = c => c.Tags.Contains(InfrastructureServiceCollectionExtensions.ReadyTag),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});

// ponytail: migração no startup serve para dev/compose com uma instância só.
// Com várias réplicas, desligar a flag e aplicar via `dotnet ef migrations bundle` num job de deploy.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.Run();

public partial class Program; // exposto para WebApplicationFactory nos testes de integração
