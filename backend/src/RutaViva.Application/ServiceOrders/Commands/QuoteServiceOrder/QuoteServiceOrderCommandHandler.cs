using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Abstractions.Time;
using RutaViva.Application.ServiceOrders.Models;
using RutaViva.Domain.ValueObjects;

namespace RutaViva.Application.ServiceOrders.Commands.QuoteServiceOrder;

public sealed class QuoteServiceOrderCommandHandler(
    IServiceOrderRepository orders,
    IOutboxWriter outbox,
    IClock clock)
    : IRequestHandler<QuoteServiceOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(QuoteServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.Quote(new MoneyCop(command.Amount));
        await ServiceOrderCommandSupport.PersistAsync(order, original, outbox, clock, orders, cancellationToken);
        return order.ToDto();
    }
}
