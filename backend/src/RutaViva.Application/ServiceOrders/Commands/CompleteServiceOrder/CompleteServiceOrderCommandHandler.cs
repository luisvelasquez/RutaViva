using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Application.ServiceOrders.Models;
using RutaViva.Domain.Events;
using RutaViva.Domain.ValueObjects;

namespace RutaViva.Application.ServiceOrders.Commands.CompleteServiceOrder;

public sealed class CompleteServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IVehicleRepository vehicles,
    DueMaintenanceCoordinator coordinator,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<CompleteServiceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CompleteServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var orderVersion = order.Version;

        var km = new Kilometrage(command.Kilometres);
        var date = command.Date;

        var routine = await routines.FindRoutineByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        var vehicle = await vehicles.FindVehicleByIdAsync(order.VehicleId, cancellationToken);
        var vehicleVersion = vehicle.Version;

        order.CompleteService(km, date);
        routine.ResetMaintenanceCycle(km, date, clock.Today);
        vehicle.RegisterOdometerReading(km);

        await outbox.FlushAsync(order, clock.UtcNow, cancellationToken);
        await orders.SaveServiceOrderAsync(order, orderVersion, cancellationToken);
        await routines.SaveRoutineAsync(routine, routineVersion, cancellationToken);
        if (vehicle.Version != vehicleVersion)
            await vehicles.SaveVehicleAsync(vehicle, vehicleVersion, cancellationToken);

        foreach (var other in await routines.FindRoutinesByVehicleAsync(vehicle.Id, cancellationToken))
        {
            if (other.Id == routine.Id)
                continue;
            var otherVersion = other.Version;
            await coordinator.EvaluateRoutineAsync(vehicle, other, cancellationToken);
            if (other.Version != otherVersion)
                await routines.SaveRoutineAsync(other, otherVersion, cancellationToken);
        }

        await outbox.AppendAsync(
            new DomainEvent(DomainEventTypes.OrderStateChanged, order.Id, order.CycleId, $"Orden en estado {order.State}."),
            clock.UtcNow,
            cancellationToken);

        return order.ToDto();
    }
}
