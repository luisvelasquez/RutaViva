using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.MaintenanceOverview.Dtos;
using RutaViva.Application.MaintenanceOverview.Queries;

namespace RutaViva.Application.MaintenanceOverview.Handlers;

public sealed class GetMaintenanceOverviewQueryHandler(IMaintenanceOverviewReader readStore)
    : IRequestHandler<GetMaintenanceOverviewQuery, MaintenanceOverviewDto>
{
    public Task<MaintenanceOverviewDto> Handle(
        GetMaintenanceOverviewQuery request,
        CancellationToken cancellationToken) =>
        readStore.GetOverviewAsync(cancellationToken);
}
