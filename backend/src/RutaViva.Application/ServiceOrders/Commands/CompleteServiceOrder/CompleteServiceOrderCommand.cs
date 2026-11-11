using RutaViva.Application.Messaging;
using RutaViva.Application.ServiceOrders.Models;

namespace RutaViva.Application.ServiceOrders.Commands.CompleteServiceOrder;

public sealed record CompleteServiceOrderCommand(
    Guid OrderId,
    long ExpectedVersion,
    decimal Kilometres,
    DateOnly Date,
    IdempotencyContext Idempotency) : IIdempotentCommand<OrderDto>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
