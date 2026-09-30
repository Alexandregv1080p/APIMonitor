using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiMonitor.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController(StatisticsService statistics) : ControllerBase
{
    /// <summary>Visão agregada: status atual de todos os endpoints e métricas das últimas 24h.</summary>
    [HttpGet]
    public Task<DashboardResponse> Get(CancellationToken ct) => statistics.GetDashboardAsync(ct);
}
