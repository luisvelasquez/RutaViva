using RutaViva.Domain.Aggregates.ServiceOrders;

namespace RutaViva.Application.ServiceOrders.Models;

public static class OrderMapping
{
    public static OrderDto ToDto(this ServiceOrder order) =>
        new(
            order.Id,
            order.VehicleId,
            order.RoutineId,
            order.Workshop,
            order.State.ToString(),
            order.QuotedAmount?.Amount,
            order.ActualKm?.Value,
            order.ActualDate,
            order.Version,
            order.CreatedAt);
}
