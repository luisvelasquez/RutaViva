namespace RutaViva.Application.Abstractions.Time;

public interface IClock
{
    DateOnly Today { get; }
    DateTimeOffset UtcNow { get; }
}
