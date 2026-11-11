using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.ServiceOrders.Models;

namespace RutaViva.Application.ServiceOrders.Commands.ApproveServiceOrder;

public sealed class ApproveServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<ApproveServiceOrderCommand, OrderDto>
{
    public Task<OrderDto> Handle(ApproveServiceOrderCommand command, CancellationToken cancellationToken) =>
        ServiceOrderCommandSupport.TransitionAsync(
            orders, outbox, clock, command.OrderId, command.ExpectedVersion, o => o.ApproveQuote(), cancellationToken);
}
