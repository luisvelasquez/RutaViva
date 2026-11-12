namespace RutaViva.Domain.Events;

public static class DomainEventFactory
{
    public static DomainEvent MaintenanceDueSoon(Guid routineId, Guid cycleId, string routineName) =>
        new(DomainEventTypes.MaintenanceDueSoon, routineId, cycleId, $"{routineName}: mantenimiento por vencer.");

    public static DomainEvent ServiceOrderCreated(Guid orderId, Guid cycleId, string routineName, string plate) =>
        new(DomainEventTypes.ServiceOrderCreated, orderId, cycleId, $"Orden creada para {routineName}, vehículo {plate}.");

    public static DomainEvent ServiceCompleted(Guid orderId, Guid cycleId) =>
        new(DomainEventTypes.ServiceCompleted, orderId, cycleId, "Servicio completado y rutina reiniciada.");

    public static DomainEvent OrderStateChanged(Guid orderId, Guid cycleId, string state) =>
        new(DomainEventTypes.OrderStateChanged, orderId, cycleId, $"Orden en estado {state}.");
}
