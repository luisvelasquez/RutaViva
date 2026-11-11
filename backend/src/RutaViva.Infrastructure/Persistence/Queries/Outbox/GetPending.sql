SELECT TOP (@take)
    Id,
    Type,
    AggregateId,
    CycleId,
    Message,
    OccurredAt,
    PublishedAt
FROM dbo.Outbox
WHERE PublishedAt IS NULL
ORDER BY OccurredAt;
