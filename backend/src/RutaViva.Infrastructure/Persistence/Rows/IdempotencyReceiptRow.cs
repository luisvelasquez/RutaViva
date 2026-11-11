namespace RutaViva.Infrastructure.Persistence.Rows;

public sealed class IdempotencyReceiptRow
{
    public string Fingerprint { get; init; } = "";
    public string Response { get; init; } = "";
}
