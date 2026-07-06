namespace CapitalPos.Tests.Integration;

public sealed class PostgreSqlIntegrationFactAttribute : FactAttribute
{
    public PostgreSqlIntegrationFactAttribute()
    {
        if (!PostgreSqlTestDatabase.HasConnectionString)
        {
            Skip = "Prueba de integracion omitida: defina CAPITALPOS_TEST_CONNECTION_STRING para usar una base PostgreSQL exclusiva de pruebas.";
        }
    }
}
