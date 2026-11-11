using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.ServiceOrders.Models;

namespace RutaViva.Application.ServiceOrders.Commands.RejectServiceOrder;

public sealed class RejectServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<RejectServiceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(RejectServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.RejectQuote();

        var routine = await routines.FindRoutineByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        routine.SuppressCancelledCycle();
        await routines.SaveRoutineAsync(routine, routineVersion, cancellationToken);

        await ServiceOrderCommandSupport.PersistAsync(order, original, outbox, clock, orders, cancellationToken);
        return order.ToDto();
    }
}
