using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.ServiceOrders.Models;

namespace RutaViva.Application.ServiceOrders.Commands.CancelServiceOrder;

public sealed class CancelServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<CancelServiceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CancelServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.Cancel();

        var routine = await routines.FindRoutineByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        routine.SuppressCancelledCycle();
        await routines.SaveRoutineAsync(routine, routineVersion, cancellationToken);

        await ServiceOrderCommandSupport.PersistAsync(order, original, outbox, clock, orders, cancellationToken);
        return order.ToDto();
    }
}
