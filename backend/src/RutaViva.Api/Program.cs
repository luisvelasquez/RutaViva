using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RutaViva.Api;
using RutaViva.Application;
using RutaViva.Application.Maintenance;
using RutaViva.Application.Orders;
using RutaViva.Application.Routines;
using RutaViva.Application.Vehicles;
using RutaViva.Domain;
using RutaViva.Infrastructure;

if (args.Contains("--healthcheck"))
{
    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
        var response = await client.GetAsync("http://localhost:8080/health");
        Environment.ExitCode = response.IsSuccessStatusCode ? 0 : 1;
    }
    catch
    {
        Environment.ExitCode = 1;
    }
    return;
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRutaViva(builder.Configuration);
builder.Services.AddHostedService<MaintenanceWorker>();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:4200", "http://localhost:8080").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseMiddleware<ErrorMiddleware>();
app.UseCors();
app.MapOpenApi();

app.MapGet("/health", async (SqlConnectionFactory factory, CancellationToken ct) =>
{
    await using var connection = factory.Create();
    await connection.OpenAsync(ct);
    return Results.Ok(new { status = "ready" });
});

app.MapGet("/api/dashboard", (IReadModel read, CancellationToken ct) => read.GetDashboardAsync(ct));

app.MapPost("/api/vehicles", (
    HttpRequest request,
    CreateVehicleCommand input,
    CreateVehicleHandler handler,
    IIdempotentExecutor tx,
    CancellationToken ct) =>
    Run(request, tx, input, [], async token => (object)await handler.HandleAsync(input, token), ct));

app.MapPost("/api/vehicles/{id:guid}/routines", (
    Guid id,
    HttpRequest request,
    CreateRoutineCommand input,
    CreateMaintenanceRoutineHandler handler,
    IIdempotentExecutor tx,
    CancellationToken ct) =>
    Run(request, tx, input, [$"vehicle:{id:N}"], async token =>
        new { id = await handler.HandleAsync(id, input, token) }, ct));

app.MapPost("/api/vehicles/{id:guid}/readings", (
    Guid id,
    HttpRequest request,
    RegisterReadingCommand input,
    RegisterOdometerReadingHandler handler,
    IIdempotentExecutor tx,
    CancellationToken ct) =>
    Run(request, tx, input, [$"vehicle:{id:N}"], token => handler.HandleAsync(id, input, token), ct));

app.MapPost("/api/maintenance/evaluate", (
    HttpRequest request,
    EvaluateDueMaintenanceHandler handler,
    IIdempotentExecutor tx,
    CancellationToken ct) =>
    Run(request, tx, new { }, [], token => handler.HandleAsync(token), ct));

app.MapPost("/api/orders/{id:guid}/{action}", async (
    Guid id,
    string action,
    HttpRequest request,
    OrderActionCommand input,
    QuoteServiceOrderHandler quote,
    ApproveServiceOrderHandler approve,
    RejectServiceOrderHandler reject,
    CancelServiceOrderHandler cancel,
    StartServiceOrderHandler start,
    CompleteServiceOrderHandler complete,
    IIdempotentExecutor tx,
    CancellationToken ct) =>
{
    return await Run(request, tx, input, [$"order:{id:N}"], async token => (object)(action switch
    {
        "quote" => await quote.HandleAsync(id, input, token),
        "approve" => await approve.HandleAsync(id, input, token),
        "reject" => await reject.HandleAsync(id, input, token),
        "cancel" => await cancel.HandleAsync(id, input, token),
        "start" => await start.HandleAsync(id, input, token),
        "complete" => await complete.HandleAsync(id, input, token),
        _ => throw new BusinessException("Acción desconocida.")
    }), ct);
});

await DatabaseBootstrap.Initialize(app.Services, app.Configuration);
app.Run();

static async Task<IResult> Run<T>(
    HttpRequest request,
    IIdempotentExecutor tx,
    T input,
    IEnumerable<string> locks,
    Func<CancellationToken, Task<object>> action,
    CancellationToken ct)
{
    var json = JsonSerializer.Serialize(input, JsonSerializerOptions.Web);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    var scope = request.Path.Value!;
    var result = await tx.ExecuteAsync(
        scope,
        request.Headers["Idempotency-Key"].ToString(),
        hash,
        locks,
        async token => JsonSerializer.Serialize(await action(token), JsonSerializerOptions.Web),
        ct);
    return Results.Content(result, "application/json");
}

public partial class Program { }
