SELECT
    RequestFingerprint,
    StoredResponse
FROM dbo.IdempotencyRecords
WHERE Scope = @scope
  AND IdempotencyKey = @key;
