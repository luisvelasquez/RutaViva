using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Application.Interfaces.Time;
using RutaViva.Application.MaintenanceRoutines.Coordination;
using RutaViva.Domain;
using RutaViva.Domain.Aggregates.MaintenanceRoutines;
using RutaViva.Domain.Aggregates.Vehicles;
using RutaViva.Domain.ValueObjects;
using RutaViva.Infrastructure.Persistence.Concurrency;
using RutaViva.Infrastructure.Persistence.Connections;
using RutaViva.Infrastructure.Persistence.Idempotency;
using RutaViva.Infrastructure.Persistence.Repositories;
using RutaViva.Infrastructure.Persistence.Transactions;
using RutaViva.Tests.Support;
using Xunit;
using RutaViva.Application.Messaging.Idempotency;
using RutaViva.Application.ServiceOrders.Commands;
using RutaViva.Application.ServiceOrders.Handlers;
using RutaViva.Application.MaintenanceRoutines.Commands;
using RutaViva.Application.MaintenanceRoutines.Handlers;
using RutaViva.Application.Vehicles.Commands;
using RutaViva.Application.Vehicles.Handlers;

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
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerOptions.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

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
        await TestDatabase.InitializeAsync(Factory());
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
        public IdempotentExecutor Executor { get; }
        public RegisterOdometerReadingCommandHandler RegisterReading { get; }
        public EvaluateDueMaintenanceCommandHandler Evaluate { get; }
        public QuoteServiceOrderCommandHandler Quote { get; }
        public StartServiceOrderCommandHandler Start { get; }
        public CancelServiceOrderCommandHandler Cancel { get; }
        public CompleteServiceOrderCommandHandler Complete { get; }
        public VehicleRepository Vehicles { get; }
        public MaintenanceRoutineRepository Routines { get; }

        public Scope(SqlConnectionFactory factory, IClock clock)
        {
            UnitOfWork = new SqlUnitOfWork(factory);
            Vehicles = new VehicleRepository(UnitOfWork);
            Routines = new MaintenanceRoutineRepository(UnitOfWork);
            var orders = new ServiceOrderRepository(UnitOfWork);
            var outbox = new OutboxWriter(UnitOfWork);
            var idempotency = new IdempotencyStore(UnitOfWork);
            var locks = new ApplicationLock(UnitOfWork);
            var coordinator = new DueMaintenanceCoordinator(orders, outbox, clock);
            RegisterReading = new RegisterOdometerReadingCommandHandler(Vehicles, Routines, coordinator, locks);
            Evaluate = new EvaluateDueMaintenanceCommandHandler(Vehicles, Routines, coordinator, locks, clock);
            Quote = new QuoteServiceOrderCommandHandler(orders, outbox, clock);
            Start = new StartServiceOrderCommandHandler(orders, outbox, clock);
            Cancel = new CancelServiceOrderCommandHandler(orders, Routines, outbox, clock);
            Complete = new CompleteServiceOrderCommandHandler(orders, Routines, Vehicles, coordinator, outbox, locks, clock);
            Executor = new IdempotentExecutor(UnitOfWork, idempotency, locks, clock);
        }

        public ValueTask DisposeAsync() => UnitOfWork.DisposeAsync();
    }

    private static IdempotencyContext Ctx(string scope, string key, string fingerprint) =>
        new(scope, key, fingerprint, []);

    private async Task<(Guid Vehicle, Guid Routine)> Seed(decimal current = 0)
    {
        await using var scope = new Scope(Factory(), clock);
        Guid vehicleId = default;
        Guid routineId = default;
        await scope.Executor.ExecuteAsync("seed", "one", "v1", [], async ct =>
        {
            var v = new Vehicle("TEST01", "Taller", new Kilometres(current));
            var r = new MaintenanceRoutine(v.Id, "Aceite", new(100, null), new(0), clock.Today);
            await scope.Vehicles.InsertVehicleAsync(v, ct);
            await scope.Routines.InsertRoutineAsync(r, ct);
            vehicleId = v.Id;
            routineId = r.Id;
            return true;
        }, CancellationToken.None);
        return (vehicleId, routineId);
    }

    private async Task<string> Command(string key, Func<Scope, CancellationToken, Task<object>> action, string fingerprint = "same")
    {
        await using var scope = new Scope(Factory(), clock);
        var result = await scope.Executor.ExecuteAsync(
            "test",
            key,
            fingerprint,
            [],
            async ct => await action(scope, ct),
            CancellationToken.None);
        // Misma política que IdempotentExecutor: el replay debe comparar bytes idénticos.
        return JsonSerializer.Serialize(result, JsonOptions);
    }

    [SqlFact]
    public async Task Concurrent_same_key_executes_once_and_replays_identical_response()
    {
        var ids = await Seed();
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ =>
            Command("repeat", async (s, ct) => await s.RegisterReading.Handle(
                new RegisterOdometerReadingCommand(ids.Vehicle, 100, Ctx("test", "repeat", "same")), ct))));
        Assert.Single(results.Distinct());
        await using var db = Db();
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ServiceOrders"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.IdempotencyRecords WHERE Scope='test'"));
    }

    [SqlFact]
    public async Task Concurrent_different_readings_and_daily_review_keep_maximum_and_single_order()
    {
        var ids = await Seed();
        var tasks = Enumerable.Range(100, 12)
            .Select(n => Command("reading" + n, async (s, ct) => await s.RegisterReading.Handle(
                new RegisterOdometerReadingCommand(ids.Vehicle, n, Ctx("test", "reading" + n, "same")), ct)))
            .ToList();
        tasks.Add(Command("daily", async (s, ct) => await s.Evaluate.Handle(
            new EvaluateDueMaintenanceCommand(Ctx("test", "daily", "same")), ct)));
        await Task.WhenAll(tasks);

        await using var db = Db();
        Assert.Equal(111, await db.ExecuteScalarAsync<decimal>("SELECT OdometerKilometers FROM dbo.Vehicles"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.ServiceOrders"));
    }

    [SqlFact]
    public async Task Concurrent_warning_evaluation_publishes_one_event_per_cycle()
    {
        await Seed(90);
        await Task.WhenAll(Enumerable.Range(0, 10).Select(n =>
            Command("daily" + n, async (s, ct) => await s.Evaluate.Handle(
                new EvaluateDueMaintenanceCommand(Ctx("test", "daily" + n, "same")), ct))));

        await using var db = Db();
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OutboxMessages WHERE EventType='MaintenanceDueSoon'"));
    }

    [SqlFact]
    public async Task Same_key_with_different_payload_conflicts()
    {
        var ids = await Seed();
        await Command("key", async (s, ct) => await s.RegisterReading.Handle(
            new RegisterOdometerReadingCommand(ids.Vehicle, 10, Ctx("test", "key", "same")), ct));
        await Assert.ThrowsAsync<ConflictException>(() =>
            Command("key", async (s, ct) => await s.RegisterReading.Handle(
                new RegisterOdometerReadingCommand(ids.Vehicle, 20, Ctx("test", "key", "different")), ct), "different"));

        await using var db = Db();
        Assert.Equal(10, await db.ExecuteScalarAsync<decimal>("SELECT OdometerKilometers FROM dbo.Vehicles"));
    }

    [SqlFact]
    public async Task Database_rejects_duplicate_open_order_even_outside_application()
    {
        var ids = await Seed(100);
        await Command("daily", async (s, ct) => await s.Evaluate.Handle(
            new EvaluateDueMaintenanceCommand(Ctx("test", "daily", "same")), ct));
        await using var db = Db();
        var cycleId = await db.ExecuteScalarAsync<Guid>("SELECT MaintenanceCycleId FROM dbo.MaintenanceRoutines");
        await Assert.ThrowsAsync<SqlException>(() => db.ExecuteAsync("""
            INSERT dbo.ServiceOrders(ServiceOrderId,Version,VehicleId,MaintenanceRoutineId,MaintenanceCycleId,AssignedWorkshop,Status,CreatedAt)
            VALUES(@id,1,@Vehicle,@Routine,@cycle,'Taller','Created',@now)
            """, new { id = Guid.NewGuid(), ids.Vehicle, ids.Routine, cycle = cycleId, now = clock.UtcNow }));
    }

    [SqlFact]
    public async Task Failed_completion_rolls_back_order_routine_receipt_and_events()
    {
        await Seed(100);
        await Command("daily", async (s, ct) => await s.Evaluate.Handle(
            new EvaluateDueMaintenanceCommand(Ctx("test", "daily", "same")), ct));
        await using var query = Db();
        var orderId = await query.ExecuteScalarAsync<Guid>("SELECT ServiceOrderId FROM dbo.ServiceOrders");

        await Command("quote", async (s, ct) => await s.Quote.Handle(
            new QuoteServiceOrderCommand(orderId, 1, 100, Ctx("test", "quote", "same")), ct));
        await Command("start", async (s, ct) => await s.Start.Handle(
            new StartServiceOrderCommand(orderId, 2, Ctx("test", "start", "same")), ct));
        await Assert.ThrowsAsync<BusinessException>(() => Command("bad-complete", async (s, ct) =>
            await s.Complete.Handle(
                new CompleteServiceOrderCommand(orderId, 3, 100, clock.Today.AddDays(1), Ctx("test", "bad-complete", "same")), ct)));

        await using var db = Db();
        Assert.Equal("InProgress", await db.ExecuteScalarAsync<string>("SELECT Status FROM dbo.ServiceOrders"));
        Assert.Equal(0, await db.ExecuteScalarAsync<decimal>("SELECT LastServiceOdometerKilometers FROM dbo.MaintenanceRoutines"));
        Assert.False(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.IdempotencyRecords WHERE IdempotencyKey='bad-complete'") > 0);
        Assert.False(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OutboxMessages WHERE EventType='ServiceCompleted'") > 0);
    }

    [SqlFact]
    public async Task Concurrent_start_and_cancel_only_one_transition_succeeds()
    {
        await Seed(100);
        await Command("daily", async (s, ct) => await s.Evaluate.Handle(
            new EvaluateDueMaintenanceCommand(Ctx("test", "daily", "same")), ct));
        await using var query = Db();
        var id = await query.ExecuteScalarAsync<Guid>("SELECT ServiceOrderId FROM dbo.ServiceOrders");
        await Command("quote", async (s, ct) => await s.Quote.Handle(
            new QuoteServiceOrderCommand(id, 1, 100, Ctx("test", "quote", "same")), ct));

        async Task<bool> Try(string action)
        {
            try
            {
                if (action == "start")
                    await Command(action, async (s, ct) => await s.Start.Handle(
                        new StartServiceOrderCommand(id, 2, Ctx("test", action, "same")), ct));
                else
                    await Command(action, async (s, ct) => await s.Cancel.Handle(
                        new CancelServiceOrderCommand(id, 2, Ctx("test", action, "same")), ct));
                return true;
            }
            catch (ConflictException) { return false; }
            catch (BusinessException) { return false; }
        }

        var results = await Task.WhenAll(Try("start"), Try("cancel"));
        Assert.Equal(1, results.Count(x => x));
    }

    [SqlFact]
    public async Task Successful_completion_resets_cycle_and_replay_does_not_reset_again()
    {
        await Seed(100);
        await Command("daily", async (s, ct) => await s.Evaluate.Handle(
            new EvaluateDueMaintenanceCommand(Ctx("test", "daily", "same")), ct));
        await using var query = Db();
        var id = await query.ExecuteScalarAsync<Guid>("SELECT ServiceOrderId FROM dbo.ServiceOrders");
        await Command("quote", async (s, ct) => await s.Quote.Handle(
            new QuoteServiceOrderCommand(id, 1, 100, Ctx("test", "quote", "same")), ct));
        await Command("start", async (s, ct) => await s.Start.Handle(
            new StartServiceOrderCommand(id, 2, Ctx("test", "start", "same")), ct));

        await using var before = Db();
        var cycleBeforeComplete = await before.ExecuteScalarAsync<Guid>("SELECT MaintenanceCycleId FROM dbo.MaintenanceRoutines");

        var first = await Command("complete", async (s, ct) => await s.Complete.Handle(
            new CompleteServiceOrderCommand(id, 3, 110, clock.Today, Ctx("test", "complete", "same")), ct));

        await using var mid = Db();
        var cycleAfterComplete = await mid.ExecuteScalarAsync<Guid>("SELECT MaintenanceCycleId FROM dbo.MaintenanceRoutines");
        Assert.NotEqual(cycleBeforeComplete, cycleAfterComplete);

        var replay = await Command("complete", async (s, ct) => await s.Complete.Handle(
            new CompleteServiceOrderCommand(id, 3, 110, clock.Today, Ctx("test", "complete", "same")), ct));

        // Contrato: replay reproduce bytes JSON (UnsafeRelaxedJsonEscaping) y no vuelve a reiniciar.
        Assert.Equal(first, replay);
        var firstDto = JsonSerializer.Deserialize<JsonElement>(first);
        var replayDto = JsonSerializer.Deserialize<JsonElement>(replay);
        Assert.Equal(firstDto.GetProperty("id").GetGuid(), replayDto.GetProperty("id").GetGuid());
        Assert.Equal(firstDto.GetProperty("state").GetString(), replayDto.GetProperty("state").GetString());
        Assert.Equal(firstDto.GetProperty("version").GetInt64(), replayDto.GetProperty("version").GetInt64());

        await using var db = Db();
        Assert.Equal(cycleAfterComplete, await db.ExecuteScalarAsync<Guid>("SELECT MaintenanceCycleId FROM dbo.MaintenanceRoutines"));
        Assert.Equal(110, await db.ExecuteScalarAsync<decimal>("SELECT LastServiceOdometerKilometers FROM dbo.MaintenanceRoutines"));
        Assert.Equal("Current", await db.ExecuteScalarAsync<string>("SELECT Status FROM dbo.MaintenanceRoutines"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.OutboxMessages WHERE EventType='ServiceCompleted'"));
        Assert.Equal(1, await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.IdempotencyRecords WHERE IdempotencyKey='complete'"));
    }

    [SqlFact]
    public async Task Concurrent_complete_and_reading_serialize_on_vehicle_lock()
    {
        await Seed(100);
        await Command("daily", async (s, ct) => await s.Evaluate.Handle(
            new EvaluateDueMaintenanceCommand(Ctx("test", "daily", "same")), ct));
        await using var query = Db();
        var id = await query.ExecuteScalarAsync<Guid>("SELECT ServiceOrderId FROM dbo.ServiceOrders");
        var vehicleId = await query.ExecuteScalarAsync<Guid>("SELECT VehicleId FROM dbo.ServiceOrders");
        await Command("quote", async (s, ct) => await s.Quote.Handle(
            new QuoteServiceOrderCommand(id, 1, 100, Ctx("test", "quote", "same")), ct));
        await Command("start", async (s, ct) => await s.Start.Handle(
            new StartServiceOrderCommand(id, 2, Ctx("test", "start", "same")), ct));

        var complete = Command("complete-x", async (s, ct) => await s.Complete.Handle(
            new CompleteServiceOrderCommand(id, 3, 120, clock.Today, Ctx("test", "complete-x", "same")), ct));
        var reading = Command("reading-x", async (s, ct) => await s.RegisterReading.Handle(
            new RegisterOdometerReadingCommand(vehicleId, 115, Ctx("test", "reading-x", "same")), ct));

        await Task.WhenAll(complete, reading);

        await using var db = Db();
        Assert.Equal("Completed", await db.ExecuteScalarAsync<string>("SELECT Status FROM dbo.ServiceOrders"));
        Assert.Equal(120, await db.ExecuteScalarAsync<decimal>("SELECT OdometerKilometers FROM dbo.Vehicles"));
    }

    [SqlFact]
    public async Task Sql_queries_are_parameterized_not_interpolated()
    {
        var sql = RutaViva.Infrastructure.Persistence.SqlQueryCatalog.Get(
            RutaViva.Infrastructure.Persistence.SqlQueryCatalog.Vehicles.GetById);
        Assert.Contains("@id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);

        await using var scope = new Scope(Factory(), clock);
        await scope.UnitOfWork.BeginAsync(CancellationToken.None);
        await scope.UnitOfWork.RollbackAsync(CancellationToken.None);
    }
}
