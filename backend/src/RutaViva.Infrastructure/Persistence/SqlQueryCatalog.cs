using System.Collections.Concurrent;
using System.Reflection;

namespace RutaViva.Infrastructure.Persistence;

/// <summary>
/// Lee consultas .sql embebidas. Los nombres están controlados por la aplicación, nunca por HTTP.
/// </summary>
public static class SqlQueryCatalog
{
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);
    private static readonly Assembly Assembly = typeof(SqlQueryCatalog).Assembly;

    public static class Vehicles
    {
        public const string GetById = "Vehicles.GetById";
        public const string ListIds = "Vehicles.ListIds";
        public const string Insert = "Vehicles.Insert";
        public const string Update = "Vehicles.Update";
    }

    public static class MaintenanceRoutines
    {
        public const string GetById = "MaintenanceRoutines.GetById";
        public const string GetByVehicle = "MaintenanceRoutines.GetByVehicle";
        public const string Insert = "MaintenanceRoutines.Insert";
        public const string Update = "MaintenanceRoutines.Update";
    }

    public static class ServiceOrders
    {
        public const string GetById = "ServiceOrders.GetById";
        public const string FindOpen = "ServiceOrders.FindOpen";
        public const string Insert = "ServiceOrders.Insert";
        public const string Update = "ServiceOrders.Update";
    }

    public static class Idempotency
    {
        public const string GetReceipt = "Idempotency.GetReceipt";
        public const string InsertReceipt = "Idempotency.InsertReceipt";
    }

    public static class Outbox
    {
        public const string Insert = "Outbox.Insert";
        public const string GetPending = "Outbox.GetPending";
        public const string MarkPublished = "Outbox.MarkPublished";
    }

    public static class Dashboard
    {
        public const string Load = "Dashboard.Load";
    }

    public static string Get(string queryName)
    {
        if (string.IsNullOrWhiteSpace(queryName) || queryName.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Nombre de consulta no permitido.");

        return Cache.GetOrAdd(queryName, static name =>
        {
            var resource = Assembly.GetManifestResourceNames()
                .SingleOrDefault(n => n.EndsWith($".Queries.{name}.sql", StringComparison.OrdinalIgnoreCase)
                    || n.EndsWith($"Queries.{name}.sql", StringComparison.OrdinalIgnoreCase)
                    || Matches(n, name));

            if (resource is null)
                throw new InvalidOperationException($"Consulta SQL no encontrada: {name}");

            using var stream = Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException($"No se pudo abrir {resource}");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
    }

    private static bool Matches(string resourceName, string queryName)
    {
        // Embebido típico: RutaViva.Infrastructure.Persistence.Queries.Vehicles.GetById.sql
        var needle = ".Persistence.Queries." + queryName.Replace('/', '.') + ".sql";
        return resourceName.EndsWith(needle, StringComparison.OrdinalIgnoreCase)
            || resourceName.Contains(queryName.Replace('/', '.') + ".sql", StringComparison.OrdinalIgnoreCase);
    }
}
