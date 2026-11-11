using RutaViva.Application.Messaging.Interfaces;
using RutaViva.Application.Vehicles.Dtos;

namespace RutaViva.Application.Vehicles.Queries;

public sealed record GetVehicleByIdQuery(Guid VehicleId) : IQuery<VehicleDto>;

public sealed record ListVehiclesQuery : IQuery<IReadOnlyList<VehicleDto>>;
