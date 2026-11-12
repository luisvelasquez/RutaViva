using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Domain.Aggregates.ServiceOrders;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Domain.Events;

namespace RutaViva.Application.MaintenanceRoutines.Coordination;

public sealed class DueMaintenanceCoordinator(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
{
    public async Task EvaluateRoutineAsync(Vehicle vehicle, MaintenanceRoutine routine, CancellationToken cancellationToken)
    {
        routine.EvaluateMaintenanceDueDates(vehicle.Odometer, clock.Today);
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
            DomainEventFactory.ServiceOrderCreated(order.Id, routine.CycleId, routine.Name, vehicle.Plate),
            clock.UtcNow,
            cancellationToken);
    }
}
