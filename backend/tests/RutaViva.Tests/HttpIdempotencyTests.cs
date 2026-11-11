using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace RutaViva.Tests;

/// <summary>
/// Verifica el contrato HTTP de Idempotency-Key sin SQL real:
/// la validación falla antes de tocar la base cuando falta la cabecera.
/// </summary>
public sealed class HttpIdempotencyTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public HttpIdempotencyTests(WebApplicationFactory<Program> factory)
    {
        var customized = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] =
                        "Server=127.0.0.1,1433;Database=RutaVivaHttpTest;User Id=sa;Password=Unused!;TrustServerCertificate=True;Encrypt=True"
                });
            });
            builder.ConfigureServices(services =>
            {
                foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                    services.Remove(descriptor);
            });
        });
        client = customized.CreateClient();
    }

    [Fact]
    public async Task Post_without_Idempotency_Key_returns_422()
    {
        var response = await client.PostAsJsonAsync("/api/vehicles", new
        {
            plate = "ABC123",
            workshop = "Taller",
            odometer = 0
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Idempotency-Key", body, StringComparison.OrdinalIgnoreCase);
    }
}
