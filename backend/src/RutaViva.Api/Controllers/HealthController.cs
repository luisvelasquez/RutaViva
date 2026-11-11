using Microsoft.AspNetCore.Mvc;
using RutaViva.Infrastructure.Persistence.Connections;

namespace RutaViva.Api.Controllers;

[ApiController]
[Tags("Health")]
public sealed class HealthController(SqlConnectionFactory factory) : ControllerBase
{
    [HttpGet("/health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        await using var connection = factory.Create();
        await connection.OpenAsync(cancellationToken);
        return Ok(new { status = "ready" });
    }
}
