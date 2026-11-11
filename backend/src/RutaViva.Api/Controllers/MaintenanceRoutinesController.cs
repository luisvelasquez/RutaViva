using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Requests.MaintenanceRoutines;
using RutaViva.Application.MaintenanceRoutines.Commands;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/vehicles/{vehicleId:guid}/routines")]
[Tags("MaintenanceRoutines")]
public sealed class MaintenanceRoutinesController(IMediator mediator) : ControllerBase
{
    /// <summary>Crea una rutina del vehículo y evalúa su estado inicial.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateMaintenanceRoutineResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<CreateMaintenanceRoutineResult> CreateMaintenanceRoutine(
        Guid vehicleId,
        [FromBody] CreateMaintenanceRoutineRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new CreateMaintenanceRoutineCommand(
            vehicleId,
            request.Name,
            request.IntervalKm,
            request.IntervalDays,
            request.LastServiceKm,
            request.LastServiceDate,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"vehicle:{vehicleId:N}")), cancellationToken);
}
