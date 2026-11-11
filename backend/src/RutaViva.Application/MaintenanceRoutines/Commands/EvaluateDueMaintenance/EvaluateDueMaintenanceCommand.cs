using RutaViva.Application.Messaging;

namespace RutaViva.Application.MaintenanceRoutines.Commands.EvaluateDueMaintenance;

public sealed record EvaluateDueMaintenanceResult(int Evaluated, DateOnly Date);

public sealed record EvaluateDueMaintenanceCommand(IdempotencyContext Idempotency)
    : IIdempotentCommand<EvaluateDueMaintenanceResult>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
