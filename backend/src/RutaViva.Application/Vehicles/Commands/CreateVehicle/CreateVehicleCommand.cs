using RutaViva.Application.Messaging;
using RutaViva.Application.Vehicles.Models;

namespace RutaViva.Application.Vehicles.Commands.CreateVehicle;

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
