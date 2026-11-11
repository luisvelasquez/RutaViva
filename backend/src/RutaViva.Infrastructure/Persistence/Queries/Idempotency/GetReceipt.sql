SELECT
    Fingerprint,
    Response
FROM dbo.Receipts
WHERE Scope = @scope
  AND [Key] = @key;
