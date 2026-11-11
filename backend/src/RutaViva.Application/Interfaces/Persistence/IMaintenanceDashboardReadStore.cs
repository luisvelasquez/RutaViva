using RutaViva.Application.Dashboard.Models;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IMaintenanceDashboardReadStore
{
    Task<MaintenanceDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}
