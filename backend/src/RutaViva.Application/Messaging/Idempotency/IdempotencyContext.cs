namespace RutaViva.Application.Messaging.Idempotency;

public sealed record IdempotencyContext(
    string Scope,
    string Key,
    string ContentFingerprint,
    IReadOnlyList<string> ResourceLocks);
