namespace RutaViva.Application.ServiceOrders.Models;

public sealed record OrderDto(
    Guid Id,
    Guid VehicleId,
    Guid RoutineId,
    string Workshop,
    string State,
    decimal? QuoteAmount,
    decimal? ActualKm,
    DateOnly? ActualDate,
    long Version,
    DateTimeOffset CreatedAt);
