using RutaViva.Domain;

namespace RutaViva.Application;

public interface IClock
{
    DateOnly Today { get; }
    DateTimeOffset UtcNow { get; }
}

public interface IUnitOfWork : IAsyncDisposable
{
    Task EnsureOpenAsync(CancellationToken cancellationToken);
    Task BeginAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}

public interface IVehicleRepository
{
    Task<Vehicle> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Guid>> ListIdsAsync(int skip, int take, CancellationToken cancellationToken);
    Task InsertAsync(Vehicle vehicle, CancellationToken cancellationToken);
    Task UpdateAsync(Vehicle vehicle, long expectedVersion, CancellationToken cancellationToken);
}

public interface IMaintenanceRoutineRepository
{
    Task<MaintenanceRoutine> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MaintenanceRoutine>> GetByVehicleAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task InsertAsync(MaintenanceRoutine routine, CancellationToken cancellationToken);
    Task UpdateAsync(MaintenanceRoutine routine, long expectedVersion, CancellationToken cancellationToken);
}

public interface IServiceOrderRepository
{
    Task<ServiceOrder> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> HasOpenAsync(Guid vehicleId, Guid routineId, CancellationToken cancellationToken);
    Task InsertAsync(ServiceOrder order, CancellationToken cancellationToken);
    Task UpdateAsync(ServiceOrder order, long expectedVersion, CancellationToken cancellationToken);
}

public interface IOutboxWriter
{
    Task AppendAsync(DomainEvent domainEvent, DateTimeOffset occurredAt, CancellationToken cancellationToken);
}

public interface IIdempotencyStore
{
    Task AcquireScopeLockAsync(string scope, string key, CancellationToken cancellationToken);
    Task AcquireResourceLockAsync(string resource, CancellationToken cancellationToken);
    Task<IdempotencyReceipt?> GetReceiptAsync(string scope, string key, CancellationToken cancellationToken);
    Task InsertReceiptAsync(string scope, string key, string fingerprint, string response, DateTimeOffset createdAt, CancellationToken cancellationToken);
}

public interface IReadModel
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
}

public sealed record IdempotencyReceipt(string Fingerprint, string Response);

public interface IIdempotentExecutor
{
    Task<string> ExecuteAsync(
        string scope,
        string key,
        string fingerprint,
        IEnumerable<string> resourceLocks,
        Func<CancellationToken, Task<string>> action,
        CancellationToken cancellationToken);
}
