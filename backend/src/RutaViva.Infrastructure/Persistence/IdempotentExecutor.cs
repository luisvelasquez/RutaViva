using RutaViva.Application;
using RutaViva.Domain;

namespace RutaViva.Infrastructure.Persistence;

/// <summary>
/// Idempotencia durable distinta de la concurrencia de negocio:
/// - Idempotencia: misma clave + misma huella => mismo resultado; distinta huella => conflicto.
/// - Concurrencia: locks por recurso + Version + índices únicos.
/// </summary>
public sealed class IdempotentExecutor(
    SqlUnitOfWork unitOfWork,
    IIdempotencyStore store,
    IClock clock) : IIdempotentExecutor
{
    public async Task<string> ExecuteAsync(
        string scope,
        string key,
        string fingerprint,
        IEnumerable<string> resourceLocks,
        Func<CancellationToken, Task<string>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
            throw new BusinessException("Idempotency-Key es obligatorio y admite hasta 100 caracteres.");

        await unitOfWork.BeginAsync(cancellationToken);
        try
        {
            // Orden estable: primero idempotencia, luego recursos ordenados alfabéticamente.
            await store.AcquireScopeLockAsync(scope, key, cancellationToken);
            foreach (var resource in resourceLocks.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
                await store.AcquireResourceLockAsync(resource, cancellationToken);

            var receipt = await store.GetReceiptAsync(scope, key, cancellationToken);
            if (receipt is not null)
            {
                if (receipt.Fingerprint != fingerprint)
                    throw new ConflictException("La clave de idempotencia ya se utilizó con otros datos.");
                await unitOfWork.CommitAsync(cancellationToken);
                return receipt.Response;
            }

            var response = await action(cancellationToken);
            await store.InsertReceiptAsync(scope, key, fingerprint, response, clock.UtcNow, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}
