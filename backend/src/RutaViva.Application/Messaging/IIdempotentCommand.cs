namespace RutaViva.Application.Messaging;

/// <summary>
/// Comando de escritura protegido por Idempotency-Key.
/// El pipeline calcula recibo/huella/transacción; el handler solo orquesta dominio.
/// </summary>
public interface IIdempotentCommand<out TResponse> : ICommand<TResponse>
{
    string IdempotencyScope { get; }
    string IdempotencyKey { get; }
    string ContentFingerprint { get; }
    IReadOnlyList<string> ResourceLocks { get; }
}
