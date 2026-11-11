using RutaViva.Api;
using RutaViva.Application;
using RutaViva.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHostedService<MaintenanceWorker>();
        services.AddCors(o => o.AddDefaultPolicy(p =>
            p.WithOrigins("http://localhost:4200", "http://localhost:8080")
                .AllowAnyHeader()
                .AllowAnyMethod()));

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new()
                {
                    Title = "RutaViva API",
                    Version = "v1",
                    Description = "API de mantenimiento preventivo de flotas (reto RutaViva)."
                };
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static IServiceCollection AddRutaVivaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddApi();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services;
    }
}
