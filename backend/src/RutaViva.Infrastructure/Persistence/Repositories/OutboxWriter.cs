using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application.Interfaces.Events;
using RutaViva.Domain.Events;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public sealed class OutboxWriter(SqlUnitOfWork unitOfWork) : SqlRepositoryBase(unitOfWork), IOutboxWriter
{
    public async Task AppendAsync(
        DomainEvent domainEvent,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        try
        {
            await UnitOfWork.Connection.ExecuteAsync(Command(
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
                cancellationToken));
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627
            && domainEvent.Type == DomainEventTypes.MaintenanceDueSoon)
        {
            // Un aviso por ciclo: el índice único filtrado absorbe la carrera.
        }
    }
}
