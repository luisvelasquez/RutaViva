using RutaViva.Application.Messaging;
using RutaViva.Application.ServiceOrders.Models;

namespace RutaViva.Application.ServiceOrders.Commands.ApproveServiceOrder;

public sealed record ApproveServiceOrderCommand(
    Guid OrderId,
    long ExpectedVersion,
    IdempotencyContext Idempotency) : IIdempotentCommand<OrderDto>
{
    public string IdempotencyScope => Idempotency.Scope;
    public string IdempotencyKey => Idempotency.Key;
    public string ContentFingerprint => Idempotency.ContentFingerprint;
    public IReadOnlyList<string> ResourceLocks => Idempotency.ResourceLocks;
}
