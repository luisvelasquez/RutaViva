using Dapper;
using RutaViva.Application.Interfaces.Idempotency;
using RutaViva.Infrastructure.Persistence.Models;
using RutaViva.Infrastructure.Persistence.Repositories;
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
        var recordData = await UnitOfWork.Connection.QuerySingleOrDefaultAsync<IdempotencyRecordPersistenceModel>(Command(
            SqlQueryCatalog.Get(SqlQueryCatalog.Idempotency.GetReceipt),
            new { scope, key },
            cancellationToken));
        return recordData is null
            ? null
            : new IdempotencyReceipt(recordData.RequestFingerprint, recordData.StoredResponse);
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
