namespace RutaViva.Application.Interfaces.Time;

public interface IClock
{
    DateOnly Today { get; }
    DateTimeOffset UtcNow { get; }
}
