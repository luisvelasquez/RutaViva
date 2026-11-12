namespace RutaViva.Application.Interfaces.Concurrency;

public interface IApplicationLock
{
    Task AcquireScopeLockAsync(string scope, string key, CancellationToken cancellationToken);
    Task AcquireResourceLockAsync(string resource, CancellationToken cancellationToken);
}
