using ApiMonitor.Application.DTOs;
using ApiMonitor.Application.Services;
using ApiMonitor.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ApiMonitor.Api.Controllers;

[ApiController]
[Route("api/endpoints")]
[Produces("application/json")]
public class EndpointsController(EndpointService service, CheckService checks, StatisticsService statistics) : ControllerBase
{
    /// <summary>Uptime, latência e série temporal. period: 24h (padrão), 7d ou 30d.</summary>
    [HttpGet("{id:guid}/statistics")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<EndpointStatisticsResponse> Statistics(Guid id, [FromQuery] string? period, CancellationToken ct) =>
        statistics.GetEndpointStatisticsAsync(id, period, ct);

    /// <summary>Dispara uma verificação imediata, fora do agendamento.</summary>
    [HttpPost("{id:guid}/check")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<CheckResultResponse> CheckNow(Guid id, CancellationToken ct) => checks.CheckNowAsync(id, ct);

    /// <summary>Histórico paginado, mais recentes primeiro. Datas sem fuso são UTC; ambos os limites são inclusivos.</summary>
    [HttpGet("{id:guid}/checks")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<PagedResponse<CheckResultResponse>> History(Guid id, [FromQuery] CheckHistoryQuery query, CancellationToken ct) =>
        checks.GetHistoryAsync(id, query, ct);

    [HttpGet]
    public Task<IReadOnlyList<EndpointResponse>> List(
        [FromQuery] EndpointStatus? status, [FromQuery] bool? enabled, CancellationToken ct) =>
        service.ListAsync(status, enabled, ct);

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<EndpointResponse> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    public async Task<ActionResult<EndpointResponse>> Create(EndpointRequest request, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ValidationProblemDetails))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<EndpointResponse> Update(Guid id, EndpointRequest request, CancellationToken ct) =>
        service.UpdateAsync(id, request, ct);

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public Task<EndpointResponse> SetEnabled(Guid id, SetEnabledRequest request, CancellationToken ct) =>
        service.SetEnabledAsync(id, request.Enabled, ct);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
