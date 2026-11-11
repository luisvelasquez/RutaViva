using MediatR;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Dashboard.Models;

namespace RutaViva.Application.Dashboard.Queries.GetMaintenanceDashboard;

public sealed class GetMaintenanceDashboardQueryHandler(IMaintenanceDashboardReadStore readStore)
    : IRequestHandler<GetMaintenanceDashboardQuery, MaintenanceDashboardDto>
{
    public Task<MaintenanceDashboardDto> Handle(
        GetMaintenanceDashboardQuery request,
        CancellationToken cancellationToken) =>
        readStore.GetDashboardAsync(cancellationToken);
}
