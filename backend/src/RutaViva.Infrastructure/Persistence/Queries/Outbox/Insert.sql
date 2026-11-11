INSERT INTO dbo.Outbox (Id, Type, AggregateId, CycleId, Message, OccurredAt, PublishedAt)
VALUES (@Id, @Type, @AggregateId, @CycleId, @Message, @OccurredAt, @PublishedAt);
