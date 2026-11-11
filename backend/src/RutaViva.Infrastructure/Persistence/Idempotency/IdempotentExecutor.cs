using System.Text.Encodings.Web;
using System.Text.Json;
using RutaViva.Application.Interfaces.Concurrency;
using RutaViva.Application.Interfaces.Idempotency;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Domain;

namespace RutaViva.Infrastructure.Persistence.Idempotency;

/// <summary>
/// Idempotencia durable (recibo + huella). Serializa con escapado relajado para que
/// el replay reproduzca el mismo JSON que la primera respuesta (p. ej. +00:00).
/// </summary>
public sealed class IdempotentExecutor(
    IUnitOfWork unitOfWork,
    IIdempotencyStore store,
    IApplicationLock applicationLock,
    IClock clock) : IIdempotentExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<T> ExecuteAsync<T>(
        string scope,
        string key,
        string fingerprint,
        IEnumerable<string> resourceLocks,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 100)
            throw new BusinessException("Idempotency-Key es obligatorio y admite hasta 100 caracteres.");

        await unitOfWork.BeginAsync(cancellationToken);
        try
        {
            await applicationLock.AcquireScopeLockAsync(scope, key, cancellationToken);
            foreach (var resource in resourceLocks.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
                await applicationLock.AcquireResourceLockAsync(resource, cancellationToken);

            var receipt = await store.GetReceiptAsync(scope, key, cancellationToken);
            if (receipt is not null)
            {
                if (receipt.Fingerprint != fingerprint)
                    throw new ConflictException("La clave de idempotencia ya se utilizó con otros datos.");
                await unitOfWork.CommitAsync(cancellationToken);
                return JsonSerializer.Deserialize<T>(receipt.Response, JsonOptions)!;
            }

            var response = await action(cancellationToken);
            var json = JsonSerializer.Serialize(response, JsonOptions);
            await store.InsertReceiptAsync(scope, key, fingerprint, json, clock.UtcNow, cancellationToken);
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
