using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application;
using RutaViva.Application.Maintenance;
using RutaViva.Application.Orders;
using RutaViva.Application.Vehicles;
using RutaViva.Domain;
using RutaViva.Infrastructure;
using RutaViva.Infrastructure.Persistence;
using RutaViva.Infrastructure.Persistence.Repositories;
using Xunit;

namespace RutaViva.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RUTAVIVA_TEST_SQL")))
            Skip = "Configure RUTAVIVA_TEST_SQL para ejecutar contra SQL Server real.";
    }
}

public sealed class FixedClock : IClock
{
    public DateOnly Today => new(2026, 11, 10);
    public DateTimeOffset UtcNow => new(2026, 11, 10, 12, 0, 0, TimeSpan.Zero);
}

public sealed class SqlIntegrationTests : IAsyncLifetime
{
    private readonly FixedClock clock = new();
    private string connection = "";

    private SqlConnectionFactory Factory() => new(connection);
    private SqlConnection Db() => Factory().Create();

    public async Task InitializeAsync()
    {
        var source = Environment.GetEnvironmentVariable("RUTAVIVA_TEST_SQL");
        if (source is null) return;

        var cs = new SqlConnectionStringBuilder(source)
        {
            InitialCatalog = "RutaVivaTest_" + Guid.NewGuid().ToString("N")
        };
        connection = cs.ConnectionString;
        await SqlDatabase.Initialize(Factory());
    }

    public async Task DisposeAsync()
    {
        if (connection.Length == 0) return;
        var cs = new SqlConnectionStringBuilder(connection);
        var database = cs.InitialCatalog;
        cs.InitialCatalog = "master";
        SqlConnection.ClearAllPools();
        await using var db = new SqlConnection(cs.ConnectionString);
        await db.ExecuteAsync($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}]");
    }

    private sealed class Scope : IAsyncDisposable
    {
        public SqlUnitOfWork UnitOfWork { get; }
        public VehicleRepository Vehicles { get; }
        public MaintenanceRoutineRepository Routines { get; }
        public ServiceOrderRepository Orders { get; }
        public OutboxWriter Outbox { get; }
        public IdempotencyStore Idempotency { get; }
        public DueMaintenanceCoordinator Coordinator { get; }
        public RegisterOdometerReadingHandler RegisterReading { get; }
        public EvaluateDueMaintenanceHandler Evaluate { get; }
        public QuoteServiceOrderHandler Quote { get; }
        public StartServiceOrderHandler Start { get; }
        public CancelServiceOrderHandler Cancel { get; }
        public CompleteServiceOrderHandler Complete { get; }
        public IdempotentExecutor Executor { get; }

        public Scope(SqlConnectionFactory factory, IClock clock)
        {
            UnitOfWork = new SqlUnitOfWork(factory);
            Vehicles = new VehicleRepository(UnitOfWork);
            Routines = new MaintenanceRoutineRepository(UnitOfWork);
            Orders = new ServiceOrderRepository(UnitOfWork);
            Outbox = new OutboxWriter(UnitOfWork);
            Idempotency = new IdempotencyStore(UnitOfWork);
            Coordinator = new DueMaintenanceCoordinator(Orders, Outbox, clock);
            RegisterReading = new RegisterOdometerReadingHandler(Vehicles, Routines, Coordinator);
            Evaluate = new EvaluateDueMaintenanceHandler(Vehicles, Routines, Coordinator, Idempotency, clock);
            Quote = new QuoteServiceOrderHandler(Orders, Outbox, clock);
            Start = new StartServiceOrderHandler(Orders, Outbox, clock);
            Cancel = new CancelServiceOrderHandler(Orders, Routines, Outbox, clock);
            Complete = new CompleteServiceOrderHandler(Orders, Routines, Vehicles, Coordinator, Outbox, clock);
            Executor = new IdempotentExecutor(UnitOfWork, Idempotency, clock);
        }

        public ValueTask DisposeAsync() => UnitOfWork.DisposeAsync();
    }

    private async Task<(Guid Vehicle, Guid Routine)> Seed(decimal current = 0)
    {
        await using var scope = new Scope(Factory(), clock);
        Guid vehicleId = default;
        Guid routineId = default;
        await scope.Executor.ExecuteAsync("seed", "one", "v1", [], async ct =>
        {
            var v = new Vehicle("TEST01", "Taller", new Kilometrage(current));
            var r = new MaintenanceRoutine(v.Id, "Aceite", new(100, null), new(0), clock.Today);
            await scope.Vehicles.InsertAsync(v, ct);
            await scope.Routines.InsertAsync(r, ct);
            vehicleId = v.Id;
            routineId = r.Id;
            return "{}";
        }, CancellationToken.None);
        return (vehicleId, routineId);
    }

    private async Task<string> Command(string key, Func<Scope, CancellationToken, Task<object>> action, string fingerprint = "same")
    {
        await using var scope = new Scope(Factory(), clock);
        return await scope.Executor.ExecuteAsync(
            "test",
            key,
            fingerprint,
            [],
            async ct => JsonSerializer.Serialize(await action(scope, ct)),
            CancellationToken.None);
    }

    [SqlFact]
    public async Task Concurrent_same_key_executes_once_and_replays_identical_response()
    {
        var ids = await Seed();
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            Command("repeat", (s, ct) => s.RegisterReading.HandleAsync(ids.Vehicle, new(100), ct))));
        Assert.Single(results.Distinct());
        await using var db = Db();
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Orders"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Receipts WHERE Scope='test'"));
    }

    [SqlFact]
    public async Task Concurrent_different_readings_and_daily_review_keep_maximum_and_single_order()
    {
        var ids = await Seed();
        var tasks = Enumerable.Range(100, 12)
            .Select(n => Command("reading" + n, (s, ct) => s.RegisterReading.HandleAsync(ids.Vehicle, new(n), ct)))
            .ToList();
        tasks.Add(Command("daily", (s, ct) => s.Evaluate.HandleAsync(ct)));
        await Task.WhenAll(tasks);

        await using var db = Db();
        Assert.Equal(111, await db.ExecuteScalarAsync<decimal>("SELECT Odometer FROM dbo.Vehicles"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Orders"));
    }

    [SqlFact]
    public async Task Concurrent_warning_evaluation_publishes_one_event_per_cycle()
    {
        await Seed(90);
        await Task.WhenAll(Enumerable.Range(0, 10).Select(n =>
            Command("daily" + n, (s, ct) => s.Evaluate.HandleAsync(ct))));

        await using var db = Db();
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Outbox WHERE Type='MaintenanceDueSoon'"));
    }

    [SqlFact]
    public async Task Same_key_with_different_payload_conflicts()
    {
        var ids = await Seed();
        await Command("key", (s, ct) => s.RegisterReading.HandleAsync(ids.Vehicle, new(10), ct));
        await Assert.ThrowsAsync<ConflictException>(() =>
            Command("key", (s, ct) => s.RegisterReading.HandleAsync(ids.Vehicle, new(20), ct), "different"));

        await using var db = Db();
        Assert.Equal(10, await db.ExecuteScalarAsync<decimal>("SELECT Odometer FROM dbo.Vehicles"));
    }

    [SqlFact]
    public async Task Database_rejects_duplicate_open_order_even_outside_application()
    {
        var ids = await Seed(100);
        await Command("daily", (s, ct) => s.Evaluate.HandleAsync(ct));
        await using var db = Db();
        var cycleId = await db.ExecuteScalarAsync<Guid>("SELECT CycleId FROM dbo.Routines");
        await Assert.ThrowsAsync<SqlException>(() => db.ExecuteAsync("""
            INSERT dbo.Orders(Id,Version,VehicleId,RoutineId,CycleId,Workshop,State,CreatedAt)
            VALUES(@id,1,@Vehicle,@Routine,@cycle,'Taller','Created',@now)
            """, new { id = Guid.NewGuid(), ids.Vehicle, ids.Routine, cycle = cycleId, now = clock.UtcNow }));
    }

    [SqlFact]
    public async Task Failed_completion_rolls_back_order_routine_receipt_and_events()
    {
        var ids = await Seed(100);
        await Command("daily", (s, ct) => s.Evaluate.HandleAsync(ct));
        await using var query = Db();
        var orderId = await query.ExecuteScalarAsync<Guid>("SELECT Id FROM dbo.Orders");

        await Command("quote", async (s, ct) => await s.Quote.HandleAsync(orderId, new(1, 100), ct));
        await Command("start", async (s, ct) => await s.Start.HandleAsync(orderId, new(2), ct));
        await Assert.ThrowsAsync<BusinessException>(() => Command("bad-complete", async (s, ct) =>
            await s.Complete.HandleAsync(orderId, new(3, Kilometres: 100, Date: clock.Today.AddDays(1)), ct)));

        await using var db = Db();
        Assert.Equal("InProgress", await db.ExecuteScalarAsync<string>("SELECT State FROM dbo.Orders"));
        Assert.Equal(0, await db.ExecuteScalarAsync<decimal>("SELECT LastServiceKm FROM dbo.Routines"));
        Assert.False(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Receipts WHERE [Key]='bad-complete'") > 0);
        Assert.False(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Outbox WHERE Type='ServiceCompleted'") > 0);
    }

    [SqlFact]
    public async Task Concurrent_start_and_cancel_only_one_transition_succeeds()
    {
        await Seed(100);
        await Command("daily", (s, ct) => s.Evaluate.HandleAsync(ct));
        await using var query = Db();
        var id = await query.ExecuteScalarAsync<Guid>("SELECT Id FROM dbo.Orders");
        await Command("quote", async (s, ct) => await s.Quote.HandleAsync(id, new(1, 100), ct));

        async Task<bool> Try(string action)
        {
            try
            {
                if (action == "start")
                    await Command(action, async (s, ct) => await s.Start.HandleAsync(id, new(2), ct));
                else
                    await Command(action, async (s, ct) => await s.Cancel.HandleAsync(id, new(2), ct));
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
            catch (BusinessException)
            {
                return false;
            }
        }

        var results = await Task.WhenAll(Try("start"), Try("cancel"));
        Assert.Equal(1, results.Count(x => x));
    }

    [SqlFact]
    public async Task Successful_completion_resets_cycle_and_replay_does_not_reset_again()
    {
        await Seed(100);
        await Command("daily", (s, ct) => s.Evaluate.HandleAsync(ct));
        await using var query = Db();
        var id = await query.ExecuteScalarAsync<Guid>("SELECT Id FROM dbo.Orders");
        await Command("quote", async (s, ct) => await s.Quote.HandleAsync(id, new(1, 100), ct));
        await Command("start", async (s, ct) => await s.Start.HandleAsync(id, new(2), ct));

        var first = await Command("complete", async (s, ct) =>
            await s.Complete.HandleAsync(id, new(3, Kilometres: 110, Date: clock.Today), ct));
        var replay = await Command("complete", async (s, ct) =>
            await s.Complete.HandleAsync(id, new(3, Kilometres: 110, Date: clock.Today), ct));
        Assert.Equal(first, replay);

        await using var db = Db();
        Assert.Equal(110, await db.ExecuteScalarAsync<decimal>("SELECT LastServiceKm FROM dbo.Routines"));
        Assert.Equal("Current", await db.ExecuteScalarAsync<string>("SELECT Status FROM dbo.Routines"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Outbox WHERE Type='ServiceCompleted'"));
    }

    [SqlFact]
    public async Task Sql_queries_are_parameterized_not_interpolated()
    {
        var sql = SqlQueryCatalog.Get(SqlQueryCatalog.Vehicles.GetById);
        Assert.Contains("@id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("{0}", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);

        await using var scope = new Scope(Factory(), clock);
        await scope.UnitOfWork.EnsureOpenAsync(CancellationToken.None);
        // Ejecuta con parámetro Dapper: si interpolara el GUID como texto libre fallaría el plan.
        var id = Guid.NewGuid();
        await scope.UnitOfWork.Connection.QueryAsync(
            new CommandDefinition(sql, new { id }, cancellationToken: CancellationToken.None));
    }
}
