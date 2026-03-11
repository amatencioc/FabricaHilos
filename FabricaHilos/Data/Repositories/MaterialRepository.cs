using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Oracle.ManagedDataAccess.Client;

namespace FabricaHilos.Data.Repositories;

public class MaterialRepository : IMaterialRepository
{
    private readonly OracleDbContext _context;

    public MaterialRepository(OracleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Material>> ObtenerTodosAsync()
    {
        var materiales = new List<Material>();
        const string sql = @"SELECT ID, NOMBRE, DESCRIPCION, UNIDAD_MEDIDA, STOCK, COSTO_UNITARIO, ACTIVO
                             FROM MATERIALES
                             ORDER BY NOMBRE";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            materiales.Add(MapearMaterial(reader));
        }

        return materiales;
    }

    public async Task<Material?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"SELECT ID, NOMBRE, DESCRIPCION, UNIDAD_MEDIDA, STOCK, COSTO_UNITARIO, ACTIVO
                             FROM MATERIALES WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapearMaterial(reader);
        }

        return null;
    }

    public async Task<int> CrearAsync(Material material)
    {
        const string sql = @"INSERT INTO MATERIALES (NOMBRE, DESCRIPCION, UNIDAD_MEDIDA, STOCK, COSTO_UNITARIO, ACTIVO)
                             VALUES (:nombre, :descripcion, :unidadMedida, :stock, :costoUnitario, :activo)
                             RETURNING ID INTO :newId";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("nombre", material.Nombre));
        cmd.Parameters.Add(new OracleParameter("descripcion", (object?)material.Descripcion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("unidadMedida", material.UnidadMedida));
        cmd.Parameters.Add(new OracleParameter("stock", material.Stock));
        cmd.Parameters.Add(new OracleParameter("costoUnitario", material.CostoUnitario));
        cmd.Parameters.Add(new OracleParameter("activo", material.Activo ? 1 : 0));

        var newIdParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(newIdParam);

        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(newIdParam.Value);
    }

    public async Task<bool> ActualizarAsync(Material material)
    {
        const string sql = @"UPDATE MATERIALES SET
                                NOMBRE = :nombre,
                                DESCRIPCION = :descripcion,
                                UNIDAD_MEDIDA = :unidadMedida,
                                STOCK = :stock,
                                COSTO_UNITARIO = :costoUnitario,
                                ACTIVO = :activo
                             WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("nombre", material.Nombre));
        cmd.Parameters.Add(new OracleParameter("descripcion", (object?)material.Descripcion ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("unidadMedida", material.UnidadMedida));
        cmd.Parameters.Add(new OracleParameter("stock", material.Stock));
        cmd.Parameters.Add(new OracleParameter("costoUnitario", material.CostoUnitario));
        cmd.Parameters.Add(new OracleParameter("activo", material.Activo ? 1 : 0));
        cmd.Parameters.Add(new OracleParameter("id", material.Id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        const string sql = "DELETE FROM MATERIALES WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private static Material MapearMaterial(System.Data.Common.DbDataReader reader)
    {
        return new Material
        {
            Id = Convert.ToInt32(reader["ID"]),
            Nombre = reader["NOMBRE"].ToString()!,
            Descripcion = reader["DESCRIPCION"] == DBNull.Value ? null : reader["DESCRIPCION"].ToString(),
            UnidadMedida = reader["UNIDAD_MEDIDA"].ToString()!,
            Stock = Convert.ToDecimal(reader["STOCK"]),
            CostoUnitario = Convert.ToDecimal(reader["COSTO_UNITARIO"]),
            Activo = Convert.ToInt32(reader["ACTIVO"]) == 1
        };
    }
}
