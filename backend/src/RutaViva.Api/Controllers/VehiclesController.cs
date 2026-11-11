using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Contracts.Vehicles;
using RutaViva.Application.Vehicles.Commands.CreateVehicle;
using RutaViva.Application.Vehicles.Commands.RegisterOdometerReading;
using RutaViva.Application.Vehicles.Models;

namespace RutaViva.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Tags("Vehicles")]
public sealed class VehiclesController(IMediator mediator) : ControllerBase
{
    /// <summary>Registra un vehículo con taller asignado y odómetro inicial.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(VehicleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<VehicleDto> CreateVehicle(
        [FromBody] CreateVehicleRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        var command = new CreateVehicleCommand(
            request.Plate,
            request.Workshop,
            request.Odometer,
            IdempotencyHttp.Create(Request, request));
        return mediator.Send(command, cancellationToken);
    }

    /// <summary>Registra una lectura de odómetro y evalúa rutinas por kilómetros.</summary>
    [HttpPost("{id:guid}/readings")]
    [ProducesResponseType(typeof(RegisterOdometerReadingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<RegisterOdometerReadingResult> RegisterOdometerReading(
        Guid id,
        [FromBody] RegisterOdometerReadingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        _ = idempotencyKey;
        var command = new RegisterOdometerReadingCommand(
            id,
            request.Kilometres,
            IdempotencyHttp.Create(Request, request, $"vehicle:{id:N}"));
        return mediator.Send(command, cancellationToken);
    }
}
