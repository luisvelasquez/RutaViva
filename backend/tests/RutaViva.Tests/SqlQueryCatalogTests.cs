using RutaViva.Infrastructure.Persistence;
using Xunit;

namespace RutaViva.Tests;

public sealed class SqlQueryCatalogTests
{
    [Theory]
    [InlineData(SqlQueryCatalog.Vehicles.GetById, "@id")]
    [InlineData(SqlQueryCatalog.Vehicles.Insert, "@Odometer")]
    [InlineData(SqlQueryCatalog.ServiceOrders.FindOpen, "@vehicleId")]
    [InlineData(SqlQueryCatalog.Idempotency.InsertReceipt, "@fingerprint")]
    [InlineData(SqlQueryCatalog.Outbox.Insert, "@AggregateId")]
    public void Queries_are_embedded_parameterized_and_avoid_select_star(string name, string parameter)
    {
        var sql = SqlQueryCatalog.Get(name);
        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains(parameter, sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SELECT *", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("'\" +", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("${", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_or_path_traversal_names_are_rejected()
    {
        Assert.Throws<InvalidOperationException>(() => SqlQueryCatalog.Get("../secret"));
        Assert.Throws<InvalidOperationException>(() => SqlQueryCatalog.Get("Does.Not.Exist"));
    }
}
