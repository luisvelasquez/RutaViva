using Dapper;
using RutaViva.Application.Interfaces.Idempotency;
using RutaViva.Infrastructure.Persistence.Repositories;
using RutaViva.Infrastructure.Persistence.Rows;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Idempotency;

public sealed class IdempotencyStore(SqlUnitOfWork unitOfWork)
    : SqlRepositoryBase(unitOfWork), IIdempotencyStore
{
    public async Task<IdempotencyReceipt?> GetReceiptAsync(
        string scope,
        string key,
        CancellationToken cancellationToken)
    {
        var row = await UnitOfWork.Connection.QuerySingleOrDefaultAsync<ReceiptRow>(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.Idempotency.GetReceipt),
            new { scope, key },
            cancellationToken));
        return row is null ? null : new IdempotencyReceipt(row.Fingerprint, row.Response);
    }

    public Task InsertReceiptAsync(
        string scope,
        string key,
        string fingerprint,
        string response,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken) =>
        UnitOfWork.Connection.ExecuteAsync(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.Idempotency.InsertReceipt),
            new { scope, key, fingerprint, response, now = createdAt },
            cancellationToken));
}
