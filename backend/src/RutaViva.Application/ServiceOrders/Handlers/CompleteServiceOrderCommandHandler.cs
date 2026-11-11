using MediatR;
using RutaViva.Application.Interfaces.Concurrency;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Domain.Events;
using RutaViva.Domain.ValueObjects;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Mappings;
using RutaViva.Application.ServiceOrders.Commands;

namespace RutaViva.Application.ServiceOrders.Handlers;

public sealed class CompleteServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IVehicleRepository vehicles,
    DueMaintenanceCoordinator coordinator,
    IOutboxWriter outbox,
    IApplicationLock applicationLock,
    IClock clock)
    : IRequestHandler<CompleteServiceOrderCommand, ServiceOrderDto>
{
    public async Task<ServiceOrderDto> Handle(CompleteServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var lookup = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);

        await applicationLock.AcquireResourceLockAsync($"vehicle:{lookup.VehicleId:N}", cancellationToken);
        await applicationLock.AcquireResourceLockAsync($"order:{lookup.Id:N}", cancellationToken);

        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);

        var orderVersion = order.Version;
        var km = new Kilometres(command.Kilometres);
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
            DomainEventFactory.OrderStateChanged(order.Id, order.CycleId, order.State.ToString()),
            clock.UtcNow,
            cancellationToken);

        return order.ToDto();
    }
}
