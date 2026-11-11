using RutaViva.Application.Interfaces.Time;

namespace RutaViva.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "SA Pacific Standard Time" : "America/Bogota");

    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, Zone).DateTime);
}
