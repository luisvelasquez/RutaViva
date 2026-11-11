namespace RutaViva.Api.Requests.Vehicles;

public sealed record CreateVehicleRequest(string Plate, string Workshop, decimal Odometer);

public sealed record RegisterOdometerReadingRequest(decimal Kilometres);
