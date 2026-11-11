using System.Security.Cryptography;
using System.Text;
using Dapper;
using RutaViva.Application;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class IdempotencyStore(SqlUnitOfWork unitOfWork) : IIdempotencyStore
{
    public Task AcquireScopeLockAsync(string scope, string key, CancellationToken cancellationToken)
    {
        var resource = "idem:" + ShortHash(scope + "|" + key);
        return AcquireAsync(resource, cancellationToken);
    }

    public Task AcquireResourceLockAsync(string resource, CancellationToken cancellationToken) =>
        AcquireAsync(resource, cancellationToken);

    public async Task<IdempotencyReceipt?> GetReceiptAsync(string scope, string key, CancellationToken cancellationToken)
    {
        var row = await unitOfWork.Connection.QuerySingleOrDefaultAsync<ReceiptRow>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Idempotency.GetReceipt),
            new { scope, key },
            unitOfWork.Transaction,
            cancellationToken: cancellationToken));
        return row is null ? null : new IdempotencyReceipt(row.Fingerprint, row.Response);
    }

    public async Task InsertReceiptAsync(
        string scope,
        string key,
        string fingerprint,
        string response,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        await unitOfWork.Connection.ExecuteAsync(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Idempotency.InsertReceipt),
            new { scope, key, fingerprint, response, now = createdAt },
            unitOfWork.Transaction,
            cancellationToken: cancellationToken));
    }

    private async Task AcquireAsync(string resource, CancellationToken cancellationToken)
    {
        // Granularidad: recurso de idempotencia o vehículo. Dueño Transaction; se libera al commit/rollback.
        await unitOfWork.Connection.ExecuteAsync(new CommandDefinition("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            IF @result < 0 THROW 51000, 'No se pudo obtener el bloqueo de aplicación.', 1;
            """,
            new { resource = "RutaViva:" + resource },
            unitOfWork.Transaction,
            cancellationToken: cancellationToken));
    }

    private static string ShortHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 16));
    }
}
