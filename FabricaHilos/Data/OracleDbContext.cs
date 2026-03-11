using Oracle.ManagedDataAccess.Client;

namespace FabricaHilos.Data;

public class OracleDbContext
{
    private readonly string _connectionString;

    public OracleDbContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleConnection")
            ?? throw new InvalidOperationException("La cadena de conexión 'OracleConnection' no está configurada.");
    }

    public OracleConnection CreateConnection()
    {
        return new OracleConnection(_connectionString);
    }
}
