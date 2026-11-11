INSERT INTO dbo.IdempotencyRecords (
    Scope,
    IdempotencyKey,
    RequestFingerprint,
    StoredResponse,
    CreatedAt)
VALUES (
    @scope,
    @key,
    @fingerprint,
    @response,
    @now);
