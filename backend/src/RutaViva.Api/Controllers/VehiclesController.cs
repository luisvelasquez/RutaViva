using MediatR;
using Microsoft.AspNetCore.Mvc;
using RutaViva.Api.Configuration;
using RutaViva.Api.Requests.Vehicles;
using RutaViva.Application.Vehicles.Commands;
using RutaViva.Application.Vehicles.Dtos;

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
        CancellationToken cancellationToken) =>
        mediator.Send(new CreateVehicleCommand(
            request.Plate,
            request.Workshop,
            request.Odometer,
            IdempotencyHttp.Create(Request, idempotencyKey, request)), cancellationToken);

    /// <summary>Registra una lectura de odómetro y evalúa rutinas por kilómetros.</summary>
    [HttpPost("{id:guid}/readings")]
    [ProducesResponseType(typeof(RegisterOdometerReadingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public Task<RegisterOdometerReadingResult> RegisterOdometerReading(
        Guid id,
        [FromBody] RegisterOdometerReadingRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken) =>
        mediator.Send(new RegisterOdometerReadingCommand(
            id,
            request.Kilometres,
            IdempotencyHttp.Create(Request, idempotencyKey, request, $"vehicle:{id:N}")), cancellationToken);
}
