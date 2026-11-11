namespace RutaViva.Application.Interfaces.Idempotency;

public sealed record IdempotencyReceipt(string Fingerprint, string Response);
