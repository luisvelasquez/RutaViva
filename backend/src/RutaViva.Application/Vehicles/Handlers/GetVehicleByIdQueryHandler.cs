using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Vehicles.Dtos;
using RutaViva.Application.Vehicles.Queries;

namespace RutaViva.Application.Vehicles.Handlers;

public sealed class GetVehicleByIdQueryHandler(IVehicleReadStore readStore)
    : IRequestHandler<GetVehicleByIdQuery, VehicleDto>
{
    public Task<VehicleDto> Handle(GetVehicleByIdQuery request, CancellationToken cancellationToken) =>
        readStore.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
}

public sealed class ListVehiclesQueryHandler(IVehicleReadStore readStore)
    : IRequestHandler<ListVehiclesQuery, IReadOnlyList<VehicleDto>>
{
    public Task<IReadOnlyList<VehicleDto>> Handle(ListVehiclesQuery request, CancellationToken cancellationToken) =>
        readStore.ListVehiclesAsync(cancellationToken);
}
