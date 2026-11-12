using Microsoft.Data.SqlClient;

namespace RutaViva.Infrastructure.Persistence;

internal static class SqlUniqueness
{
    public static bool IsViolation(SqlException ex, string indexName) =>
        ex.Number is 2601 or 2627
        && ex.Message.Contains(indexName, StringComparison.OrdinalIgnoreCase);
}
