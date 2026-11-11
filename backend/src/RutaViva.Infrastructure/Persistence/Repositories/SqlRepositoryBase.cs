using Dapper;
using RutaViva.Infrastructure.Persistence.Transactions;

namespace RutaViva.Infrastructure.Persistence.Repositories;

/// <summary>
/// Colaboración compartida: asigna la transacción activa de la UoW y el CancellationToken.
/// No abre ni libera la conexión; eso lo hace SqlUnitOfWork.
/// </summary>
public abstract class SqlRepositoryBase(SqlUnitOfWork unitOfWork)
{
    protected SqlUnitOfWork UnitOfWork { get; } = unitOfWork;

    protected CommandDefinition Command(string sql, object? args, CancellationToken cancellationToken) =>
        new(sql, args, UnitOfWork.Transaction, cancellationToken: cancellationToken);
}
