using Dapper;
using Microsoft.Data.SqlClient;

namespace RutaViva.Infrastructure;

public static class SqlDatabase
{
    public static async Task Initialize(SqlConnectionFactory factory)
    {
        var target = new SqlConnectionStringBuilder(factory.ConnectionString);
        var database = target.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database) || database == "master")
            throw new InvalidOperationException("Configure una base de aplicación, distinta de master.");

        var master = new SqlConnectionStringBuilder(factory.ConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await connection.ExecuteAsync("""
                DECLARE @lock int;
                EXEC @lock = sys.sp_getapplock @Resource='RutaViva:create-database', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=30000;
                IF @lock < 0 THROW 51000, 'No se pudo obtener lock de creación.', 1;
                IF DB_ID(@database) IS NULL
                BEGIN
                    DECLARE @sql nvarchar(400) = N'CREATE DATABASE ' + QUOTENAME(@database);
                    EXEC sys.sp_executesql @sql;
                END;
                EXEC sys.sp_releaseapplock @Resource='RutaViva:create-database', @LockOwner='Session';
                """, new { database }, commandTimeout: 60);
        }

        await using var db = factory.Create();
        await db.OpenAsync();
        await using var transaction = await db.BeginTransactionAsync();
        await db.ExecuteAsync("""
            DECLARE @lock int;
            EXEC @lock = sys.sp_getapplock @Resource='RutaViva:schema', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=30000;
            IF @lock < 0 THROW 51000, 'No se pudo obtener lock de esquema.', 1;
            """, transaction: transaction);

        var assembly = typeof(SqlDatabase).Assembly;
        var name = assembly.GetManifestResourceNames().Single(x => x.EndsWith("001_initial.sql", StringComparison.OrdinalIgnoreCase));
        await using var stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        await db.ExecuteAsync(await reader.ReadToEndAsync(), transaction: transaction, commandTimeout: 60);
        await transaction.CommitAsync();
    }
}
