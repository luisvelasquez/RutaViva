using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using RutaViva.Domain;
using RutaViva.Application.Messaging.Idempotency;

namespace RutaViva.Api.Configuration;

public static class IdempotencyHttp
{
    public static IdempotencyContext Create(
        HttpRequest request,
        string? idempotencyKey,
        object body,
        params string[] resourceLocks)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new BusinessException("Idempotency-Key es obligatorio y admite hasta 100 caracteres.");

        var json = JsonSerializer.Serialize(body, JsonSerializerOptions.Web);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        return new IdempotencyContext(
            request.Path.Value ?? "/",
            idempotencyKey,
            fingerprint,
            resourceLocks);
    }
}
