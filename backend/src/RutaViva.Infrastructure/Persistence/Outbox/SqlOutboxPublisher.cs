using Dapper;
using Microsoft.Extensions.Logging;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Rows;

namespace RutaViva.Infrastructure.Persistence.Outbox;

/// <summary>
/// Lectura/marcado de Outbox fuera del worker de API. Reentrega al publicar ≠ aviso único por ciclo.
/// </summary>
public sealed class SqlOutboxPublisher(
    SqlConnectionFactory factory,
    IClock clock,
    ILogger<SqlOutboxPublisher> logger) : IOutboxPublisher
{
    public async Task<int> PublishPendingAsync(int take, CancellationToken cancellationToken)
    {
        await using var db = factory.Create();
        var batch = (await db.QueryAsync<OutboxMessageRow>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.GetPending),
            new { take },
            cancellationToken: cancellationToken))).ToList();

        foreach (var message in batch)
        {
            logger.LogInformation(
                "Evento {EventId}: {Type} {Message}",
                message.Id,
                message.Type,
                message.Message);
            await db.ExecuteAsync(new CommandDefinition(
                SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.MarkPublished),
                new { now = clock.UtcNow, message.Id },
                cancellationToken: cancellationToken));
        }

        return batch.Count;
    }
}
