using MediatR;
using RutaViva.Application.Interfaces.Idempotency;
using RutaViva.Application.Messaging.Interfaces;

namespace RutaViva.Application.Messaging.Behaviors;

public sealed class IdempotentCommandBehavior<TRequest, TResponse>(IIdempotentExecutor executor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken) =>
        executor.ExecuteAsync(
            request.IdempotencyScope,
            request.IdempotencyKey,
            request.ContentFingerprint,
            request.ResourceLocks,
            ct => next(),
            cancellationToken);
}
