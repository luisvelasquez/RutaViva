using Microsoft.Data.SqlClient;
using RutaViva.Application;

namespace RutaViva.Infrastructure.Persistence;

public sealed class SqlUnitOfWork(SqlConnectionFactory factory) : IUnitOfWork
{
    public SqlConnection Connection { get; } = factory.Create();
    public SqlTransaction? Transaction { get; private set; }

    public async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (Connection.State != System.Data.ConnectionState.Open)
            await Connection.OpenAsync(cancellationToken);
    }

    public async Task BeginAsync(CancellationToken cancellationToken)
    {
        await EnsureOpenAsync(cancellationToken);
        if (Transaction is not null)
            throw new InvalidOperationException("Ya hay una transacción activa.");
        Transaction = (SqlTransaction)await Connection.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (Transaction is null)
            throw new InvalidOperationException("No hay transacción para confirmar.");
        await Transaction.CommitAsync(cancellationToken);
        await Transaction.DisposeAsync();
        Transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        if (Transaction is null)
            return;
        await Transaction.RollbackAsync(cancellationToken);
        await Transaction.DisposeAsync();
        Transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (Transaction is not null)
            await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
