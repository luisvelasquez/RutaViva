using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RutaViva.Application.Messaging;
using RutaViva.Domain;

namespace RutaViva.Api.Configuration;

public static class IdempotencyHttp
{
    public static IdempotencyContext Create(
        HttpRequest request,
        object body,
        params string[] resourceLocks)
    {
        var key = request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key))
            throw new BusinessException("Idempotency-Key es obligatorio y admite hasta 100 caracteres.");

        var json = JsonSerializer.Serialize(body, JsonSerializerOptions.Web);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return new IdempotencyContext(
            request.Path.Value ?? "/",
            key,
            fingerprint,
            resourceLocks);
    }
}
