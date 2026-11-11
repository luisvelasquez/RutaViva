using System.Text.Json;
using Dapper;
using RutaViva.Application;
using RutaViva.Application.Maintenance;
using RutaViva.Infrastructure;
using RutaViva.Infrastructure.Persistence;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Api;

public sealed class MaintenanceWorker(IServiceScopeFactory scopes, ILogger<MaintenanceWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var clock = scope.ServiceProvider.GetRequiredService<IClock>();
                var tx = scope.ServiceProvider.GetRequiredService<IIdempotentExecutor>();
                var evaluate = scope.ServiceProvider.GetRequiredService<EvaluateDueMaintenanceHandler>();

                await tx.ExecuteAsync(
                    "system/daily",
                    clock.Today.ToString("yyyy-MM-dd"),
                    "daily-v1",
                    [],
                    async ct => JsonSerializer.Serialize(await evaluate.HandleAsync(ct)),
                    stoppingToken);

                var factory = scope.ServiceProvider.GetRequiredService<SqlConnectionFactory>();
                await using var db = factory.Create();
                var batch = await db.QueryAsync<OutboxRow>(new CommandDefinition(
                    SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.GetPending),
                    new { take = 100 },
                    cancellationToken: stoppingToken));

                foreach (var message in batch)
                {
                    // Entrega al menos una vez al log local.
                    logger.LogInformation("Evento {EventId}: {Type} {Message}", message.Id, message.Type, message.Message);
                    await db.ExecuteAsync(new CommandDefinition(
                        SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.MarkPublished),
                        new { now = clock.UtcNow, message.Id },
                        cancellationToken: stoppingToken));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falló evaluación diaria/publicación; se reintentará.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
