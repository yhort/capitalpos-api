using CapitalPos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CapitalPos.Tests.Integration;

public static class PostgreSqlTestDatabase
{
    private const string EnvironmentVariableName = "CAPITALPOS_TEST_CONNECTION_STRING";

    public static bool HasConnectionString =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    public static CapitalPosDbContext CreateContext()
    {
        var connectionString = GetConnectionString();
        var options = new DbContextOptionsBuilder<CapitalPosDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CapitalPosDbContext(options);
    }

    private static string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Defina {EnvironmentVariableName} para ejecutar las pruebas de integracion de persistencia.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.Database) ||
            !builder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{EnvironmentVariableName} debe apuntar a una base de datos exclusiva de pruebas. El nombre de la base debe contener 'test'.");
        }

        return connectionString;
    }
}
