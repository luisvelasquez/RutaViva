using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RutaViva.Application.Interfaces.Concurrency;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Idempotency;
using RutaViva.Application.Interfaces.Persistence;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Infrastructure.Persistence.Concurrency;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Idempotency;
using RutaViva.Infrastructure.Persistence.Outbox;
using RutaViva.Infrastructure.Persistence.Repositories;
using RutaViva.Infrastructure.Persistence.Transactions;
using RutaViva.Infrastructure.Time;

namespace RutaViva.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__Database (User Secrets o variables de entorno).");

        services.AddSingleton(new SqlConnectionFactory(connectionString));
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<SqlUnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SqlUnitOfWork>());
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IMaintenanceRoutineRepository, MaintenanceRoutineRepository>();
        services.AddScoped<IServiceOrderRepository, ServiceOrderRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddSingleton<IOutboxPublisher, SqlOutboxPublisher>();
        services.AddScoped<IIdempotencyStore, IdempotencyStore>();
        services.AddScoped<IApplicationLock, ApplicationLock>();
        services.AddScoped<IIdempotentExecutor, IdempotentExecutor>();
        services.AddScoped<IMaintenanceOverviewReader, MaintenanceOverviewReader>();
        services.AddScoped<IVehicleReadStore, VehicleReadStore>();
        services.AddScoped<IMaintenanceRoutineReadStore, MaintenanceRoutineReadStore>();
        services.AddScoped<IServiceOrderReadStore, ServiceOrderReadStore>();

        return services;
    }
}
