namespace RutaViva.Application.Interfaces.Concurrency;

/// <summary>Bloqueos de aplicación SQL con dueño Transaction.</summary>
public interface IApplicationLock
{
    Task AcquireScopeLockAsync(string scope, string key, CancellationToken cancellationToken);
    Task AcquireResourceLockAsync(string resource, CancellationToken cancellationToken);
}
