namespace RutaViva.Application.Interfaces.Idempotency;

public interface IIdempotentExecutor
{
    Task<T> ExecuteAsync<T>(
        string scope,
        string key,
        string fingerprint,
        IEnumerable<string> resourceLocks,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken);
}
