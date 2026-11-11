using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.MaintenanceRoutines.Dtos;
using RutaViva.Application.MaintenanceRoutines.Queries;

namespace RutaViva.Application.MaintenanceRoutines.Handlers;

public sealed class ListVehicleRoutinesQueryHandler(IMaintenanceRoutineReadStore readStore)
    : IRequestHandler<ListVehicleRoutinesQuery, IReadOnlyList<MaintenanceRoutineDto>>
{
    public Task<IReadOnlyList<MaintenanceRoutineDto>> Handle(
        ListVehicleRoutinesQuery request,
        CancellationToken cancellationToken) =>
        readStore.ListRoutinesByVehicleAsync(request.VehicleId, cancellationToken);
}
