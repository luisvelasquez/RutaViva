namespace RutaViva.Infrastructure.Persistence.Models;

public sealed class OutboxMessagePersistenceModel
{
    public Guid OutboxMessageId { get; init; }
    public string EventType { get; init; } = "";
    public Guid AggregateId { get; init; }
    public Guid MaintenanceCycleId { get; init; }
    public string Payload { get; init; } = "";
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}
