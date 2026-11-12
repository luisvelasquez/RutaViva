namespace RutaViva.Application.Messaging.Interfaces;

public interface IIdempotentCommand<out TResponse> : ICommand<TResponse>
{
    string IdempotencyScope { get; }
    string IdempotencyKey { get; }
    string ContentFingerprint { get; }
    IReadOnlyList<string> ResourceLocks { get; }
}
