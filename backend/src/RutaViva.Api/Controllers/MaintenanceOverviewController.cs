using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Application.MaintenanceOverview.Dtos;
using RutaViva.Application.MaintenanceOverview.Queries;
using RutaViva.Application.MaintenanceRoutines.Commands;

namespace RutaViva.Api.Controllers;

[ApiController]
[Tags("Maintenance")]
public sealed class MaintenanceOverviewController(IMediator mediator) : ControllerBase
{
    /// <summary>Read-only maintenance summary for the demo UI (vehicles, routines, orders, events).</summary>
    [HttpGet("api/dashboard")]
    [ProducesResponseType(typeof(MaintenanceOverviewDto), StatusCodes.Status200OK)]
    public Task<MaintenanceOverviewDto> GetMaintenanceDashboard(CancellationToken cancellationToken) =>
        mediator.Send(new GetMaintenanceOverviewQuery(), cancellationToken);

    /// <summary>
    /// Manually runs maintenance evaluation (same rules as the daily worker).
    /// May create due-soon notices and open service orders when routines become overdue.
    /// Prefer the worker for scheduled runs; use this endpoint for demos and troubleshooting.
    /// </summary>
    [HttpPost("api/maintenance/evaluate")]
    [ProducesResponseType(typeof(EvaluateDueMaintenanceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<EvaluateDueMaintenanceResult> EvaluateMaintenanceDueDates(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var body = new { };
        return mediator.Send(
            new EvaluateDueMaintenanceCommand(IdempotencyHttp.Create(Request, idempotencyKey, body)),
            cancellationToken);
    }
}
