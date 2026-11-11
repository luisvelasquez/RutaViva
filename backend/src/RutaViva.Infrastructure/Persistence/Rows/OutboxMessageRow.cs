namespace RutaViva.Infrastructure.Persistence.Rows;

public sealed class OutboxMessageRow
{
    public Guid Id { get; init; }
    public string Type { get; init; } = "";
    public Guid AggregateId { get; init; }
    public Guid CycleId { get; init; }
    public string Message { get; init; } = "";
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset? PublishedAt { get; init; }
}
