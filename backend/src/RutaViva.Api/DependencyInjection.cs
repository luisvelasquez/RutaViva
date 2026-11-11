using Microsoft.OpenApi;
using RutaViva.Api.Workers;
using RutaViva.Application;
using RutaViva.Infrastructure;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddProblemDetails();
        services.AddHostedService<DailyMaintenanceWorker>();
        services.AddHostedService<OutboxPublisherWorker>();
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
                    Description = "API de mantenimiento preventivo de flotas (reto RutaViva). Los POST de escritura exigen la cabecera Idempotency-Key."
                };
                return Task.CompletedTask;
            });
            options.AddOperationTransformer((operation, _, _) =>
            {
                if (operation.Parameters is null)
                    return Task.CompletedTask;

                for (var i = 0; i < operation.Parameters.Count; i++)
                {
                    var parameter = operation.Parameters[i];
                    if (!string.Equals(parameter.Name, "Idempotency-Key", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // IOpenApiParameter.Required es de solo lectura: sustituir el parámetro.
                    operation.Parameters[i] = new OpenApiParameter
                    {
                        Name = parameter.Name,
                        In = parameter.In,
                        Required = true,
                        Schema = parameter.Schema,
                        Description = parameter.Description
                            ?? "Obligatoria. Misma clave + mismos datos = replay; misma clave + datos distintos = 409."
                    };
                }

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
