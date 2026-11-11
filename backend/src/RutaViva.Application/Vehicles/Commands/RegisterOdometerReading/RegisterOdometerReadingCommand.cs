using RutaViva.Application.Messaging;
using RutaViva.Application.Vehicles.Models;

namespace RutaViva.Application.Vehicles.Commands.RegisterOdometerReading;

public sealed record RegisterOdometerReadingResult(bool Accepted, VehicleDto Vehicle);

public sealed record RegisterOdometerReadingCommand(
    Guid VehicleId,
    decimal Kilometres,
    IdempotencyContext Idempotency) : IIdempotentCommand<RegisterOdometerReadingResult>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
