namespace RutaViva.Application.ServiceOrders.Dtos;

public sealed record ServiceOrderDto(
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
