using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Requests.MaintenanceRoutines;
using RutaViva.Application.MaintenanceRoutines.Commands;
using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Application.MaintenanceRoutines.Queries;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:guid}/routines")]
[Tags("MaintenanceRoutines")]
public sealed class MaintenanceRoutinesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists maintenance routines for a vehicle (read-only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MaintenanceRoutineDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IReadOnlyList<MaintenanceRoutineDto>> ListVehicleRoutines(
        Guid vehicleId,
        CancellationToken cancellationToken) =>
        mediator.Send(new ListVehicleRoutinesQuery(vehicleId), cancellationToken);

    /// <summary>Registers a maintenance routine and evaluates its initial due status.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateMaintenanceRoutineResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CreateMaintenanceRoutineResult>> CreateMaintenanceRoutine(
        Guid vehicleId,
        [FromBody] CreateMaintenanceRoutineRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateMaintenanceRoutineCommand(
            vehicleId,
            request.Name,
            request.IntervalKm,
            request.IntervalDays,
            request.LastServiceKm,
            request.LastServiceDate,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"vehicle:{vehicleId:N}")), cancellationToken);

        return Created($"/api/vehicles/{vehicleId:D}/routines", result);
    }
}
