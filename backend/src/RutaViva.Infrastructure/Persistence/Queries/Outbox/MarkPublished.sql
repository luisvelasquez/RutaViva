UPDATE dbo.OutboxMessages
SET PublishedAt = @now
WHERE OutboxMessageId = @Id
  AND PublishedAt IS NULL;
