using MediatR;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Domain.ValueObjects;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Mappings;
using RutaViva.Application.ServiceOrders.Commands;

namespace RutaViva.Application.ServiceOrders.Handlers;

public sealed class QuoteServiceOrderCommandHandler(IServiceOrderRepository orders, IOutboxWriter outbox, IClock clock)  : IRequestHandler<QuoteServiceOrderCommand, ServiceOrderDto>
{
    public async Task<ServiceOrderDto> Handle(QuoteServiceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(command.OrderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.SubmitServiceQuote(new MoneyCop(command.Amount));
        await ServiceOrderTransitionExecutor.PersistAsync(order, original, outbox, clock, orders, cancellationToken);
        return order.ToDto();
    }
}
