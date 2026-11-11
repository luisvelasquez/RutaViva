using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application;
using RutaViva.Domain;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class OutboxWriter(SqlUnitOfWork unitOfWork) : IOutboxWriter
{
    public async Task AppendAsync(DomainEvent domainEvent, DateTimeOffset occurredAt, CancellationToken cancellationToken)
    {
        try
        {
            await unitOfWork.Connection.ExecuteAsync(new CommandDefinition(
                SqlQueryCatalog.Get(SqlQueryCatalog.Outbox.Insert),
                new
                {
                    Id = Guid.NewGuid(),
                    domainEvent.Type,
                    domainEvent.AggregateId,
                    domainEvent.CycleId,
                    domainEvent.Message,
                    OccurredAt = occurredAt,
                    PublishedAt = (DateTimeOffset?)null
                },
                unitOfWork.Transaction,
                cancellationToken: cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627
            && domainEvent.Type == DomainEventTypes.MaintenanceDueSoon)
        {
            // Un aviso por ciclo: el índice único filtrado absorbe la carrera.
        }
    }
}
