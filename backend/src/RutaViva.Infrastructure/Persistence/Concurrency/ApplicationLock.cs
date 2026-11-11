using System.Security.Cryptography;
using System.Text;
using Dapper;
using RutaViva.Application.Interfaces.Concurrency;
using RutaViva.Infrastructure.Persistence.Repositories;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Concurrency;

public sealed class ApplicationLock(SqlUnitOfWork unitOfWork)
    : SqlRepositoryBase(unitOfWork), IApplicationLock
{
    public Task AcquireScopeLockAsync(string scope, string key, CancellationToken cancellationToken) =>
        AcquireAsync("idem:" + ShortHash(scope + "|" + key), cancellationToken);

    public Task AcquireResourceLockAsync(string resource, CancellationToken cancellationToken) =>
        AcquireAsync(resource, cancellationToken);

    private async Task AcquireAsync(string resource, CancellationToken cancellationToken)
    {
        await UnitOfWork.Connection.ExecuteAsync(Command("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock
                @Resource = @resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = 15000;
            IF @result < 0 THROW 51000, 'No se pudo obtener el bloqueo de aplicación.', 1;
            """,
            new { resource = "RutaViva:" + resource },
            cancellationToken));
    }

    private static string ShortHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash.AsSpan(0, 16));
    }
}
