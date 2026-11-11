using Dapper;
using Microsoft.Data.SqlClient;
using RutaViva.Infrastructure.Persistence.Connections;

namespace RutaViva.Tests.Support;

/// <summary>Preparación de base aislada solo para pruebas de integración.</summary>
public static class TestDatabase
{
    public static async Task InitializeAsync(SqlConnectionFactory factory)
    {
        var target = new SqlConnectionStringBuilder(factory.ConnectionString);
        var database = target.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database) || database == "master")
            throw new InvalidOperationException("Configure una base de prueba distinta de master.");

        var master = new SqlConnectionStringBuilder(factory.ConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("""
                IF DB_ID(@database) IS NULL
                BEGIN
                    DECLARE @sql nvarchar(400) = N'CREATE DATABASE ' + QUOTENAME(@database);
                    EXEC sys.sp_executesql @sql;
                END;
                """, new { database }, commandTimeout: 60);
        }

        var schemaPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "database", "01_schema.sql"));
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException("No se encontró 01_schema.sql para pruebas.", schemaPath);

        var schema = await File.ReadAllTextAsync(schemaPath);
        await using var db = factory.Create();
        await db.OpenAsync();
        foreach (var batch in schema.Split(["\nGO\r", "\nGO\n", "\rGO\r", "\nGO"], StringSplitOptions.RemoveEmptyEntries))
        {
            var sql = batch.Trim();
            if (sql.Length == 0)
                continue;
            await db.ExecuteAsync(sql, commandTimeout: 60);
        }
    }
}
