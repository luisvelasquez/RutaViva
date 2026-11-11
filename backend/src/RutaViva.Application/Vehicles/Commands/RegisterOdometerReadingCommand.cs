using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Application.Messaging.Interfaces;
using RutaViva.Application.Messaging.Idempotency;

namespace RutaViva.Application.Vehicles.Commands;

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
