SELECT TOP (@take)
    OutboxMessageId,
    EventType,
    AggregateId,
    MaintenanceCycleId,
    Payload,
    OccurredAt,
    PublishedAt
FROM dbo.OutboxMessages
WHERE PublishedAt IS NULL
ORDER BY OccurredAt;
