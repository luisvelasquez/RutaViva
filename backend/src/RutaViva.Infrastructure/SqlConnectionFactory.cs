using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
namespace RutaViva.Infrastructure;
public sealed class SqlConnectionFactory(string connectionString)
{
    static SqlConnectionFactory() => SqlMapper.AddTypeHandler(new DateOnlyHandler());
    public SqlConnection Create() => new(connectionString);
    public string ConnectionString => connectionString;
    private sealed class DateOnlyHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override DateOnly Parse(object value) => value is DateTime date ? DateOnly.FromDateTime(date) : DateOnly.Parse(value.ToString()!);
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        { parameter.DbType = DbType.Date; parameter.Value = value.ToDateTime(TimeOnly.MinValue); }
    }
}
