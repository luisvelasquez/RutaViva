using Dapper;
using Microsoft.Extensions.Logging;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Models;

namespace RutaViva.Infrastructure.Persistence.Outbox;

public sealed class SqlOutboxPublisher(
    SqlConnectionFactory factory,
    IClock clock,
    ILogger<SqlOutboxPublisher> logger) : IOutboxPublisher
{
    public async Task<int> PublishPendingAsync(int take, CancellationToken cancellationToken)
    {
        await using var db = factory.Create();
        var pendingMessages = (await db.QueryAsync<OutboxMessagePersistenceModel>(new CommandDefinition(
            SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.GetPending),
            new { take },
            cancellationToken: cancellationToken))).ToList();

        foreach (var outboxMessage in pendingMessages)
        {
            logger.LogInformation(
                "Evento {EventId}: {Type} {Message}",
                outboxMessage.OutboxMessageId,
                outboxMessage.EventType,
                outboxMessage.Payload);
            await db.ExecuteAsync(new CommandDefinition(
                SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.MarkPublished),
                new { now = clock.UtcNow, Id = outboxMessage.OutboxMessageId },
                cancellationToken: cancellationToken));
        }

        return pendingMessages.Count;
    }
}
