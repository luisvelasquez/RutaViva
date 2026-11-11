using RutaViva.Application.MaintenanceOverview.Dtos;

namespace RutaViva.Application.Interfaces.Persistence;

public interface IMaintenanceOverviewReader
{
    Task<MaintenanceOverviewDto> GetOverviewAsync(CancellationToken cancellationToken);
}
