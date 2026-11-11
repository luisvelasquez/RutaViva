using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Mappings;
using RutaViva.Application.ServiceOrders.Commands;

namespace RutaViva.Application.ServiceOrders.Handlers;

public sealed class CancelServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<CancelServiceOrderCommand, ServiceOrderDto>
{
    public async Task<ServiceOrderDto> Handle(CancelServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.Cancel();

        var routine = await routines.FindRoutineByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        routine.SuppressCancelledCycle();
        await routines.SaveRoutineAsync(routine, routineVersion, cancellationToken);

        await ServiceOrderTransitionExecutor.PersistAsync(order, original, outbox, clock, orders, cancellationToken);
        return order.ToDto();
    }
}
