using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace RutaViva.Infrastructure.Persistence.Connections;

public sealed class SqlConnectionFactory(string connectionString)
{
    static SqlConnectionFactory() => SqlMapper.AddTypeHandler(new DateOnlyHandler());

    public string ConnectionString => connectionString;

    public SqlConnection Create() => new(connectionString);

    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) =>
            value is DateTime date ? DateOnly.FromDateTime(date) : DateOnly.Parse(value.ToString()!);

        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }
    }
}
