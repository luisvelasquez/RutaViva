namespace RutaViva.Application.Vehicles.Dtos;

public sealed record VehicleDto(Guid Id, string Plate, string Workshop, decimal Odometer, long Version);
