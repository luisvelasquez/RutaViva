namespace RutaViva.Application.Vehicles.Models;

public sealed record VehicleDto(Guid Id, string Plate, string Workshop, decimal Odometer, long Version);
