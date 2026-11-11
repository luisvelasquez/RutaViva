using Dapper;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

public abstract class SqlRepositoryBase(SqlUnitOfWork unitOfWork)
{
    protected SqlUnitOfWork UnitOfWork { get; } = unitOfWork;

    protected CommandDefinition Command(string sql, object? args, CancellationToken cancellationToken) =>
        new(sql, args, UnitOfWork.Transaction, cancellationToken: cancellationToken);
}
