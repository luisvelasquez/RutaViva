using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.Messaging.Interfaces;
using RutaViva.Application.Messaging.Idempotency;

namespace RutaViva.Application.ServiceOrders.Commands;

public sealed record QuoteServiceOrderCommand(
    Guid OrderId,
    long ExpectedVersion,
    decimal Amount,
    IdempotencyContext Idempotency) : IIdempotentCommand<ServiceOrderDto>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
