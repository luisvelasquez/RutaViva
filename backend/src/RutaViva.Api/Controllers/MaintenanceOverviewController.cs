using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Application.MaintenanceOverview.Dtos;
using RutaViva.Application.MaintenanceOverview.Queries;
using RutaViva.Application.MaintenanceRoutines.Commands;

namespace RutaViva.Api.Controllers;

[ApiController]
[Tags("Dashboard")]
public sealed class MaintenanceOverviewController(IMediator mediator) : ControllerBase
{
    /// <summary>Consulta de lectura del tablero de mantenimiento (sin efectos).</summary>
    [HttpGet("api/dashboard")]
    [ProducesResponseType(typeof(MaintenanceOverviewDto), StatusCodes.Status200OK)]
    public Task<MaintenanceOverviewDto> GetMaintenanceDashboard(CancellationToken cancellationToken) =>
        mediator.Send(new GetMaintenanceOverviewQuery(), cancellationToken);

    /// <summary>Ejecuta la revisión diaria por tiempo (también la dispara DailyMaintenanceWorker).</summary>
    [HttpPost("api/maintenance/evaluate")]
    [ProducesResponseType(typeof(EvaluateDueMaintenanceResult), StatusCodes.Status200OK)]
    public Task<EvaluateDueMaintenanceResult> EvaluateDueMaintenance(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var body = new { };
        return mediator.Send(
            new EvaluateDueMaintenanceCommand(IdempotencyHttp.Create(Request, idempotencyKey, body)),
            cancellationToken);
    }
}
