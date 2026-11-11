UPDATE dbo.Outbox
SET PublishedAt = @now
WHERE Id = @Id
  AND PublishedAt IS NULL;
