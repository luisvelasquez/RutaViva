using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Requests.Vehicles;
using RutaViva.Application.Vehicles.Commands;
using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Application.Vehicles.Queries;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Tags("Vehicles")]
public sealed class VehiclesController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists registered vehicles (read-only).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VehicleDto>), StatusCodes.Status200OK)]
    public Task<IReadOnlyList<VehicleDto>> ListVehicles(CancellationToken cancellationToken) =>
        mediator.Send(new ListVehiclesQuery(), cancellationToken);

    /// <summary>Reads a vehicle and its current odometer (read-only).</summary>
    [HttpGet("{vehicleId:guid}")]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<VehicleDto> GetVehicle(Guid vehicleId, CancellationToken cancellationToken) =>
        mediator.Send(new GetVehicleByIdQuery(vehicleId), cancellationToken);

    /// <summary>Registers a vehicle with assigned workshop and initial odometer.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VehicleDto>> CreateVehicle(
        [FromBody] CreateVehicleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var vehicle = await mediator.Send(new CreateVehicleCommand(
            request.Plate,
            request.Workshop,
            request.Odometer,
            IdempotencyHttp.Create(Request, idempotencyKey, request)), cancellationToken);
        return CreatedAtAction(nameof(GetVehicle), new { vehicleId = vehicle.Id }, vehicle);
    }

    /// <summary>
    /// Submits an odometer reading and evaluates kilometre-based maintenance.
    /// Readings at or below the current odometer are ignored without error (accepted=false).
    /// </summary>
    [HttpPost("{vehicleId:guid}/readings")]
    [ProducesResponseType(typeof(RegisterOdometerReadingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<RegisterOdometerReadingResult> RecordOdometerReading(
        Guid vehicleId,
        [FromBody] RegisterOdometerReadingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new RegisterOdometerReadingCommand(
            vehicleId,
            request.Kilometres,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"vehicle:{vehicleId:N}")), cancellationToken);
}
