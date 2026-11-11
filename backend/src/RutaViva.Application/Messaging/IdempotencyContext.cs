namespace RutaViva.Application.Messaging;

public sealed record IdempotencyContext(
    string Scope,
    string Key,
    string ContentFingerprint,
    IReadOnlyList<string> ResourceLocks);
