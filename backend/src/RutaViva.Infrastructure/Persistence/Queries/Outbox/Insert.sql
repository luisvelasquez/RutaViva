INSERT INTO dbo.OutboxMessages (
    OutboxMessageId,
    EventType,
    AggregateId,
    MaintenanceCycleId,
    Payload,
    OccurredAt,
    PublishedAt)
VALUES (
    @Id,
    @Type,
    @AggregateId,
    @CycleId,
    @Message,
    @OccurredAt,
    @PublishedAt);
