using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Oracle.ManagedDataAccess.Client;

namespace FabricaHilos.Data.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly OracleDbContext _context;

    public ClienteRepository(OracleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Cliente>> ObtenerTodosAsync()
    {
        var clientes = new List<Cliente>();
        const string sql = @"SELECT ID, NOMBRE, APELLIDO, EMAIL, TELEFONO, DIRECCION, FECHA_REGISTRO, ACTIVO
                             FROM CLIENTES
                             ORDER BY APELLIDO, NOMBRE";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            clientes.Add(MapearCliente(reader));
        }

        return clientes;
    }

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"SELECT ID, NOMBRE, APELLIDO, EMAIL, TELEFONO, DIRECCION, FECHA_REGISTRO, ACTIVO
                             FROM CLIENTES WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapearCliente(reader);
        }

        return null;
    }

    public async Task<int> CrearAsync(Cliente cliente)
    {
        const string sql = @"INSERT INTO CLIENTES (NOMBRE, APELLIDO, EMAIL, TELEFONO, DIRECCION, FECHA_REGISTRO, ACTIVO)
                             VALUES (:nombre, :apellido, :email, :telefono, :direccion, :fechaRegistro, :activo)
                             RETURNING ID INTO :newId";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("nombre", cliente.Nombre));
        cmd.Parameters.Add(new OracleParameter("apellido", cliente.Apellido));
        cmd.Parameters.Add(new OracleParameter("email", cliente.Email));
        cmd.Parameters.Add(new OracleParameter("telefono", (object?)cliente.Telefono ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("direccion", (object?)cliente.Direccion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("fechaRegistro", cliente.FechaRegistro));
        cmd.Parameters.Add(new OracleParameter("activo", cliente.Activo ? 1 : 0));

        var newIdParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(newIdParam);

        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(newIdParam.Value);
    }

    public async Task<bool> ActualizarAsync(Cliente cliente)
    {
        const string sql = @"UPDATE CLIENTES SET
                                NOMBRE = :nombre,
                                APELLIDO = :apellido,
                                EMAIL = :email,
                                TELEFONO = :telefono,
                                DIRECCION = :direccion,
                                ACTIVO = :activo
                             WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("nombre", cliente.Nombre));
        cmd.Parameters.Add(new OracleParameter("apellido", cliente.Apellido));
        cmd.Parameters.Add(new OracleParameter("email", cliente.Email));
        cmd.Parameters.Add(new OracleParameter("telefono", (object?)cliente.Telefono ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("direccion", (object?)cliente.Direccion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("activo", cliente.Activo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("id", cliente.Id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        const string sql = "DELETE FROM CLIENTES WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ExisteEmailAsync(string email, int? idExcluir = null)
    {
        string sql = idExcluir.HasValue
            ? "SELECT COUNT(1) FROM CLIENTES WHERE UPPER(EMAIL) = UPPER(:email) AND ID != :id"
            : "SELECT COUNT(1) FROM CLIENTES WHERE UPPER(EMAIL) = UPPER(:email)";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("email", email));
        if (idExcluir.HasValue)
        {
            cmd.Parameters.Add(new OracleParameter("id", idExcluir.Value));
        }

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    private static Cliente MapearCliente(System.Data.Common.DbDataReader reader)
    {
        return new Cliente
        {
            Id = Convert.ToInt32(reader["ID"]),
            Nombre = reader["NOMBRE"].ToString()!,
            Apellido = reader["APELLIDO"].ToString()!,
            Email = reader["EMAIL"].ToString()!,
            Telefono = reader["TELEFONO"] == DBNull.Value ? null : reader["TELEFONO"].ToString(),
            Direccion = reader["DIRECCION"] == DBNull.Value ? null : reader["DIRECCION"].ToString(),
            FechaRegistro = Convert.ToDateTime(reader["FECHA_REGISTRO"]),
            Activo = Convert.ToInt32(reader["ACTIVO"]) == 1
        };
    }
}
