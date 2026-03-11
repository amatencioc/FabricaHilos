using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Oracle.ManagedDataAccess.Client;

namespace FabricaHilos.Data.Repositories;

public class HiloRepository : IHiloRepository
{
    private readonly OracleDbContext _context;

    public HiloRepository(OracleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Hilo>> ObtenerTodosAsync()
    {
        var hilos = new List<Hilo>();
        const string sql = @"SELECT ID, CODIGO, NOMBRE, DESCRIPCION, COLOR, GRAMAJE_POR_ROLLO,
                                    STOCK, PRECIO, FECHA_CREACION, ACTIVO
                             FROM HILOS
                             ORDER BY NOMBRE";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            hilos.Add(MapearHilo(reader));
        }

        return hilos;
    }

    public async Task<Hilo?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"SELECT ID, CODIGO, NOMBRE, DESCRIPCION, COLOR, GRAMAJE_POR_ROLLO,
                                    STOCK, PRECIO, FECHA_CREACION, ACTIVO
                             FROM HILOS WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapearHilo(reader);
        }

        return null;
    }

    public async Task<int> CrearAsync(Hilo hilo)
    {
        const string sql = @"INSERT INTO HILOS (CODIGO, NOMBRE, DESCRIPCION, COLOR, GRAMAJE_POR_ROLLO,
                                                STOCK, PRECIO, FECHA_CREACION, ACTIVO)
                             VALUES (:codigo, :nombre, :descripcion, :color, :gramaje,
                                     :stock, :precio, :fechaCreacion, :activo)
                             RETURNING ID INTO :newId";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("codigo", hilo.Codigo));
        cmd.Parameters.Add(new OracleParameter("nombre", hilo.Nombre));
        cmd.Parameters.Add(new OracleParameter("descripcion", (object?)hilo.Descripcion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("color", hilo.Color));
        cmd.Parameters.Add(new OracleParameter("gramaje", hilo.GramajePorRollo));
        cmd.Parameters.Add(new OracleParameter("stock", hilo.Stock));
        cmd.Parameters.Add(new OracleParameter("precio", hilo.Precio));
        cmd.Parameters.Add(new OracleParameter("fechaCreacion", hilo.FechaCreacion));
        cmd.Parameters.Add(new OracleParameter("activo", hilo.Activo ? 1 : 0));

        var newIdParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(newIdParam);

        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(newIdParam.Value);
    }

    public async Task<bool> ActualizarAsync(Hilo hilo)
    {
        const string sql = @"UPDATE HILOS SET
                                CODIGO = :codigo,
                                NOMBRE = :nombre,
                                DESCRIPCION = :descripcion,
                                COLOR = :color,
                                GRAMAJE_POR_ROLLO = :gramaje,
                                STOCK = :stock,
                                PRECIO = :precio,
                                ACTIVO = :activo
                             WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("codigo", hilo.Codigo));
        cmd.Parameters.Add(new OracleParameter("nombre", hilo.Nombre));
        cmd.Parameters.Add(new OracleParameter("descripcion", (object?)hilo.Descripcion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("color", hilo.Color));
        cmd.Parameters.Add(new OracleParameter("gramaje", hilo.GramajePorRollo));
        cmd.Parameters.Add(new OracleParameter("stock", hilo.Stock));
        cmd.Parameters.Add(new OracleParameter("precio", hilo.Precio));
        cmd.Parameters.Add(new OracleParameter("activo", hilo.Activo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("id", hilo.Id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        const string sql = "DELETE FROM HILOS WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null)
    {
        string sql = idExcluir.HasValue
            ? "SELECT COUNT(1) FROM HILOS WHERE UPPER(CODIGO) = UPPER(:codigo) AND ID != :id"
            : "SELECT COUNT(1) FROM HILOS WHERE UPPER(CODIGO) = UPPER(:codigo)";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("codigo", codigo));
        if (idExcluir.HasValue)
        {
            cmd.Parameters.Add(new OracleParameter("id", idExcluir.Value));
        }

        var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        return count > 0;
    }

    private static Hilo MapearHilo(System.Data.Common.DbDataReader reader)
    {
        return new Hilo
        {
            Id = Convert.ToInt32(reader["ID"]),
            Codigo = reader["CODIGO"].ToString()!,
            Nombre = reader["NOMBRE"].ToString()!,
            Descripcion = reader["DESCRIPCION"] == DBNull.Value ? null : reader["DESCRIPCION"].ToString(),
            Color = reader["COLOR"].ToString()!,
            GramajePorRollo = Convert.ToDecimal(reader["GRAMAJE_POR_ROLLO"]),
            Stock = Convert.ToInt32(reader["STOCK"]),
            Precio = Convert.ToDecimal(reader["PRECIO"]),
            FechaCreacion = Convert.ToDateTime(reader["FECHA_CREACION"]),
            Activo = Convert.ToInt32(reader["ACTIVO"]) == 1
        };
    }
}
