using RutaViva.Application.Messaging;

namespace RutaViva.Application.MaintenanceRoutines.Commands.CreateMaintenanceRoutine;

public sealed record CreateMaintenanceRoutineResult(Guid Id);

public sealed record CreateMaintenanceRoutineCommand(
    Guid VehicleId,
    string Name,
    decimal? IntervalKm,
    int? IntervalDays,
    decimal LastServiceKm,
    DateOnly LastServiceDate,
    IdempotencyContext Idempotency) : IIdempotentCommand<CreateMaintenanceRoutineResult>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
