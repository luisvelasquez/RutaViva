using RutaViva.Application.Maintenance;
using RutaViva.Domain;

namespace RutaViva.Application.Orders;

public sealed class QuoteServiceOrderHandler(IServiceOrderRepository orders, IOutboxWriter outbox, IClock clock)
{
    public async Task<OrderDto> HandleAsync(Guid orderId, OrderActionCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        var amount = command.Amount ?? throw new BusinessException("Indique el valor de la cotización.");
        order.Quote(new MoneyCop(amount));
        await OrderActionSupport.PersistAsync(order, original, outbox, clock, cancellationToken, orders);
        return order.ToDto();
    }
}

public sealed class ApproveServiceOrderHandler(IServiceOrderRepository orders, IOutboxWriter outbox, IClock clock)
{
    public Task<OrderDto> HandleAsync(Guid orderId, OrderActionCommand command, CancellationToken cancellationToken) =>
        OrderActionSupport.TransitionAsync(orders, outbox, clock, orderId, command, o => o.Approve(), cancellationToken);
}

public sealed class RejectServiceOrderHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IOutboxWriter outbox,
    IClock clock)
{
    public async Task<OrderDto> HandleAsync(Guid orderId, OrderActionCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.RejectQuote();
        var routine = await routines.GetByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        routine.SuppressCancelledCycle();
        await routines.UpdateAsync(routine, routineVersion, cancellationToken);
        await OrderActionSupport.PersistAsync(order, original, outbox, clock, cancellationToken, orders);
        return order.ToDto();
    }
}

public sealed class CancelServiceOrderHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IOutboxWriter outbox,
    IClock clock)
{
    public async Task<OrderDto> HandleAsync(Guid orderId, OrderActionCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        order.Cancel();
        var routine = await routines.GetByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        routine.SuppressCancelledCycle();
        await routines.UpdateAsync(routine, routineVersion, cancellationToken);
        await OrderActionSupport.PersistAsync(order, original, outbox, clock, cancellationToken, orders);
        return order.ToDto();
    }
}

public sealed class StartServiceOrderHandler(IServiceOrderRepository orders, IOutboxWriter outbox, IClock clock)
{
    public Task<OrderDto> HandleAsync(Guid orderId, OrderActionCommand command, CancellationToken cancellationToken) =>
        OrderActionSupport.TransitionAsync(orders, outbox, clock, orderId, command, o => o.StartService(), cancellationToken);
}

public sealed class CompleteServiceOrderHandler(
    IServiceOrderRepository orders,
    IMaintenanceRoutineRepository routines,
    IVehicleRepository vehicles,
    DueMaintenanceCoordinator coordinator,
    IOutboxWriter outbox,
    IClock clock)
{
    public async Task<OrderDto> HandleAsync(Guid orderId, OrderActionCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var orderVersion = order.Version;

        var km = new Kilometrage(command.Kilometres ?? throw new BusinessException("Indique el kilometraje real."));
        var date = command.Date ?? throw new BusinessException("Indique la fecha real.");

        var routine = await routines.GetByIdAsync(order.RoutineId, cancellationToken);
        var routineVersion = routine.Version;
        var vehicle = await vehicles.GetByIdAsync(order.VehicleId, cancellationToken);
        var vehicleVersion = vehicle.Version;

        order.CompleteService(km, date);
        routine.ResetCycle(km, date, clock.Today);
        vehicle.RegisterReading(km);

        await coordinator.FlushEventsAsync(order, cancellationToken);
        await orders.UpdateAsync(order, orderVersion, cancellationToken);
        await routines.UpdateAsync(routine, routineVersion, cancellationToken);
        if (vehicle.Version != vehicleVersion)
            await vehicles.UpdateAsync(vehicle, vehicleVersion, cancellationToken);

        foreach (var other in await routines.GetByVehicleAsync(vehicle.Id, cancellationToken))
        {
            if (other.Id == routine.Id)
                continue;
            var otherVersion = other.Version;
            await coordinator.EvaluateRoutineAsync(vehicle, other, cancellationToken);
            if (other.Version != otherVersion)
                await routines.UpdateAsync(other, otherVersion, cancellationToken);
        }

        await outbox.AppendAsync(
            new DomainEvent(DomainEventTypes.OrderStateChanged, order.Id, order.CycleId, $"Orden en estado {order.State}."),
            clock.UtcNow,
            cancellationToken);

        return order.ToDto();
    }
}

internal static class OrderActionSupport
{
    public static async Task<OrderDto> TransitionAsync(
        IServiceOrderRepository orders,
        IOutboxWriter outbox,
        IClock clock,
        Guid orderId,
        OrderActionCommand command,
        Action<ServiceOrder> action,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(orderId, cancellationToken);
        order.RequireVersion(command.ExpectedVersion);
        var original = order.Version;
        action(order);
        await PersistAsync(order, original, outbox, clock, cancellationToken, orders);
        return order.ToDto();
    }

    public static async Task PersistAsync(
        ServiceOrder order,
        long originalVersion,
        IOutboxWriter outbox,
        IClock clock,
        CancellationToken cancellationToken,
        IServiceOrderRepository orders)
    {
        foreach (var domainEvent in order.DequeueDomainEvents())
            await outbox.AppendAsync(domainEvent, clock.UtcNow, cancellationToken);

        await orders.UpdateAsync(order, originalVersion, cancellationToken);
        await outbox.AppendAsync(
            new DomainEvent(DomainEventTypes.OrderStateChanged, order.Id, order.CycleId, $"Orden en estado {order.State}."),
            clock.UtcNow,
            cancellationToken);
    }
}
