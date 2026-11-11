using RutaViva.Domain.Aggregates.ServiceOrders;
using RutaViva.Application.ServiceOrders.Dtos;

namespace RutaViva.Application.ServiceOrders.Mappings;

public static class ServiceOrderMapping
{
    public static ServiceOrderDto ToDto(this ServiceOrder order) =>
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
