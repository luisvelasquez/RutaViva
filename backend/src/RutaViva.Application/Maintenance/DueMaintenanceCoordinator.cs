using RutaViva.Domain;

namespace RutaViva.Application.Maintenance;

/// <summary>
/// Coordina evaluación de una rutina y creación de orden si vence.
/// No decide umbrales: eso vive en MaintenanceRoutine.
/// </summary>
public sealed class DueMaintenanceCoordinator(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
{
    public async Task EvaluateRoutineAsync(Vehicle vehicle, MaintenanceRoutine routine, CancellationToken cancellationToken)
    {
        routine.EvaluateDue(vehicle.Odometer, clock.Today);
        await FlushEventsAsync(routine, cancellationToken);

        if (routine.Status != MaintenanceStatus.Overdue || routine.CycleCancelled)
            return;

        if (await orders.HasOpenAsync(vehicle.Id, routine.Id, cancellationToken))
            return;

        var order = new ServiceOrder(vehicle.Id, routine.Id, routine.CycleId, vehicle.Workshop, clock.UtcNow);
        try
        {
            await orders.InsertAsync(order, cancellationToken);
        }
        catch (ConflictException)
        {
            // Índice único filtrado: otra evaluación concurrente ya creó la orden abierta.
            return;
        }

        await outbox.AppendAsync(
            new DomainEvent(
                DomainEventTypes.ServiceOrderCreated,
                order.Id,
                routine.CycleId,
                $"Orden creada para {routine.Name}, vehículo {vehicle.Plate}."),
            clock.UtcNow,
            cancellationToken);
    }

    public async Task FlushEventsAsync(AggregateRoot aggregate, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in aggregate.DequeueDomainEvents())
            await outbox.AppendAsync(domainEvent, clock.UtcNow, cancellationToken);
    }
}
