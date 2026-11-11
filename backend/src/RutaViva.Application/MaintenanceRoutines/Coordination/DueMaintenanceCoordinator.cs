using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Domain.Aggregates.ServiceOrders;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Domain.Events;

namespace RutaViva.Application.MaintenanceRoutines.Coordination;

/// <summary>
/// Coordina varias raíces cuando una rutina vence: evalúa el dominio y crea la orden si corresponde.
/// </summary>
public sealed class DueMaintenanceCoordinator(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
{
    public async Task EvaluateRoutineAsync(Vehicle vehicle, MaintenanceRoutine routine, CancellationToken cancellationToken)
    {
        routine.EvaluateDueStatus(vehicle.Odometer, clock.Today);
        await outbox.FlushAsync(routine, clock.UtcNow, cancellationToken);

        if (routine.Status != MaintenanceStatus.Overdue || routine.CycleCancelled)
            return;

        if (await orders.HasOpenOrderForRoutineAsync(vehicle.Id, routine.Id, cancellationToken))
            return;

        var order = new ServiceOrder(vehicle.Id, routine.Id, routine.CycleId, vehicle.Workshop, clock.UtcNow);
        try
        {
            await orders.InsertServiceOrderAsync(order, cancellationToken);
        }
        catch (ConflictException)
        {
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
}
