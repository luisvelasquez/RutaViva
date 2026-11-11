using Microsoft.Data.SqlClient;
using RutaViva.Domain;

namespace RutaViva.Api;

public sealed class ErrorMiddleware(RequestDelegate next, ILogger<ErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, message) = ex switch
            {
                MissingException => (404, ex.Message),
                BusinessException => (422, ex.Message),
                ConflictException => (409, ex.Message),
                SqlException sql when sql.Number is 2601 or 2627 =>
                    (409, DescribeUniqueness(sql)),
                SqlException sql when sql.Number is 1205 or 51000 or -2 =>
                    (503, "Operación ocupada temporalmente. Reintente con la misma clave de idempotencia."),
                BadHttpRequestException => (400, "Revise los datos enviados."),
                _ => (500, "Ocurrió un error inesperado.")
            };

            if (status >= 500)
                logger.LogError(ex, "Error en {Path}", context.Request.Path);

            await Results.Problem(
                statusCode: status,
                title: message,
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
                .ExecuteAsync(context);
        }
    }

    private static string DescribeUniqueness(SqlException sql)
    {
        var text = sql.Message;
        if (text.Contains("UX_Vehicles_Plate", StringComparison.OrdinalIgnoreCase))
            return "Ya existe un vehículo con esa placa.";
        if (text.Contains("UX_Orders_OpenRoutine", StringComparison.OrdinalIgnoreCase))
            return "Ya existe una orden abierta para este vehículo y rutina.";
        if (text.Contains("UX_Outbox_NoticeCycle", StringComparison.OrdinalIgnoreCase))
            return "El aviso de este ciclo ya fue registrado.";
        if (text.Contains("PK_Receipts", StringComparison.OrdinalIgnoreCase))
            return "La clave de idempotencia ya está registrada.";
        // No inventar un mensaje de negocio específico para índices desconocidos.
        return "Conflicto de unicidad en los datos persistidos.";
    }
}
