using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Commands;

namespace RutaViva.Application.ServiceOrders.Handlers;

public sealed class ApproveServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<ApproveServiceOrderCommand, ServiceOrderDto>
{
    public Task<ServiceOrderDto> Handle(ApproveServiceOrderCommand command, CancellationToken cancellationToken) =>
        ServiceOrderTransitionExecutor.TransitionAsync(
            orders, outbox, clock, command.OrderId, command.ExpectedVersion, o => o.ApproveServiceQuote(), cancellationToken);
}
