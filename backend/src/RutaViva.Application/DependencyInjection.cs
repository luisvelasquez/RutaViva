using Microsoft.Extensions.DependencyInjection;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Application.Messaging;

namespace RutaViva.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            cfg.AddOpenBehavior(typeof(IdempotentCommandBehavior<,>));
        });

        services.AddScoped<DueMaintenanceCoordinator>();
        return services;
    }
}
