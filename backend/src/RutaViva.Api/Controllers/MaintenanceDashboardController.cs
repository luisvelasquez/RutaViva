using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Application.Dashboard.Models;
using RutaViva.Application.Dashboard.Queries.GetMaintenanceDashboard;
using RutaViva.Application.MaintenanceRoutines.Commands.EvaluateDueMaintenance;

namespace RutaViva.Api.Controllers;

[ApiController]
[Tags("Dashboard")]
public sealed class MaintenanceDashboardController(IMediator mediator) : ControllerBase
{
    /// <summary>Consulta de lectura del tablero de mantenimiento.</summary>
    [HttpGet("api/dashboard")]
    [ProducesResponseType(typeof(MaintenanceDashboardDto), StatusCodes.Status200OK)]
    public Task<MaintenanceDashboardDto> GetMaintenanceDashboard(CancellationToken cancellationToken) =>
        mediator.Send(new GetMaintenanceDashboardQuery(), cancellationToken);

    /// <summary>Ejecuta la revisión diaria (también la dispara el worker).</summary>
    [HttpPost("api/maintenance/evaluate")]
    [ProducesResponseType(typeof(EvaluateDueMaintenanceResult), StatusCodes.Status200OK)]
    public Task<EvaluateDueMaintenanceResult> EvaluateDueMaintenance(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        var body = new { };
        var command = new EvaluateDueMaintenanceCommand(IdempotencyHttp.Create(Request, body));
        return mediator.Send(command, cancellationToken);
    }
}
