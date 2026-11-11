namespace RutaViva.Application.Interfaces.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyReceipt?> GetReceiptAsync(string scope, string key, CancellationToken cancellationToken);
    Task InsertReceiptAsync(
        string scope,
        string key,
        string fingerprint,
        string response,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);
}
