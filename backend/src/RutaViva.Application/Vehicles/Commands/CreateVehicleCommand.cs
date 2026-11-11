using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Application.Messaging.Interfaces;
using RutaViva.Application.Messaging.Idempotency;

namespace RutaViva.Application.Vehicles.Commands;

public sealed record CreateVehicleCommand(
    string Plate,
    string Workshop,
    decimal Odometer,
    IdempotencyContext Idempotency) : IIdempotentCommand<VehicleDto>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
