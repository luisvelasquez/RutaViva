using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Domain.Aggregates.ServiceOrders;
using RutaViva.Domain.Events;
using RutaViva.Application.ServiceOrders.Dtos;
using RutaViva.Application.ServiceOrders.Mappings;

namespace RutaViva.Application.ServiceOrders.Handlers;

internal static class ServiceOrderTransitionExecutor
{
    public static async Task<ServiceOrderDto> TransitionAsync(
        IServiceOrderRepository orders,
        IOutboxWriter outbox,
        IClock clock,
        Guid orderId,
        long expectedVersion,
        Action<ServiceOrder> action,
        CancellationToken cancellationToken)
    {
        var order = await orders.FindServiceOrderByIdAsync(orderId, cancellationToken);
        order.RequireVersion(expectedVersion);
        var original = order.Version;
        action(order);
        await PersistAsync(order, original, outbox, clock, orders, cancellationToken);
        return order.ToDto();
    }

    public static async Task PersistAsync(
        ServiceOrder order,
        long originalVersion,
        IOutboxWriter outbox,
        IClock clock,
        IServiceOrderRepository orders,
        CancellationToken cancellationToken)
    {
        await outbox.FlushAsync(order, clock.UtcNow, cancellationToken);
        await orders.SaveServiceOrderAsync(order, originalVersion, cancellationToken);
        await outbox.AppendAsync(
            new DomainEvent(DomainEventTypes.OrderStateChanged, order.Id, order.CycleId, $"Orden en estado {order.State}."),
            clock.UtcNow,
            cancellationToken);
    }
}
