using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.ServiceOrders.Models;

namespace RutaViva.Application.ServiceOrders.Commands.StartServiceOrder;

public sealed class StartServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<StartServiceOrderCommand, OrderDto>
{
    public Task<OrderDto> Handle(StartServiceOrderCommand command, CancellationToken cancellationToken) =>
        ServiceOrderCommandSupport.TransitionAsync(
            orders, outbox, clock, command.OrderId, command.ExpectedVersion, o => o.StartService(), cancellationToken);
}
