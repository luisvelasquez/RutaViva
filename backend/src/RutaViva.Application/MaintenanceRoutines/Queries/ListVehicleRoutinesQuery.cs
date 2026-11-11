using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Application.Messaging.Interfaces;

namespace RutaViva.Application.MaintenanceRoutines.Queries;

public sealed record ListVehicleRoutinesQuery(Guid VehicleId) : IQuery<IReadOnlyList<MaintenanceRoutineDto>>;
