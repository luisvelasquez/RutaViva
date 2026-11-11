namespace RutaViva.Infrastructure.Persistence.Models;

public sealed class IdempotencyRecordPersistenceModel
{
    public string RequestFingerprint { get; init; } = "";
    public string StoredResponse { get; init; } = "";
}
