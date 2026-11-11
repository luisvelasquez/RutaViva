namespace RutaViva.Domain.Events;

public sealed record DomainEvent(string Type, Guid AggregateId, Guid CycleId, string Message);

public static class DomainEventTypes
{
    public const string MaintenanceDueSoon = "MaintenanceDueSoon";
    public const string ServiceOrderCreated = "ServiceOrderCreated";
    public const string ServiceCompleted = "ServiceCompleted";
    public const string OrderStateChanged = "OrderStateChanged";
}
