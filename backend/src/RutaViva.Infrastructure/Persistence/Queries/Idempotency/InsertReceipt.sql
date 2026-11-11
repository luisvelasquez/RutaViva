INSERT INTO dbo.Receipts (Scope, [Key], Fingerprint, Response, CreatedAt)
VALUES (@scope, @key, @fingerprint, @response, @now);
