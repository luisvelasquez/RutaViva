using RutaViva.Api;

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
builder.Services.AddRutaVivaServices(builder.Configuration);

var app = builder.Build();
app.UseMiddleware<ErrorMiddleware>();
app.UseCors();
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "RutaViva API v1");
    options.DocumentTitle = "RutaViva API";
    options.RoutePrefix = "swagger";
});
app.MapControllers();
app.Run();

public partial class Program;
