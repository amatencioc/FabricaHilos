using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Oracle.ManagedDataAccess.Client;

namespace FabricaHilos.Data.Repositories;

public class PedidoRepository : IPedidoRepository
{
    private readonly OracleDbContext _context;

    public PedidoRepository(OracleDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Pedido>> ObtenerTodosAsync()
    {
        var pedidos = new List<Pedido>();
        const string sql = @"SELECT p.ID, p.NUMERO, p.CLIENTE_ID, p.FECHA_PEDIDO, p.FECHA_ENTREGA,
                                    p.ESTADO, p.TOTAL, p.OBSERVACIONES,
                                    c.NOMBRE AS CLIENTE_NOMBRE, c.APELLIDO AS CLIENTE_APELLIDO
                             FROM PEDIDOS p
                             JOIN CLIENTES c ON c.ID = p.CLIENTE_ID
                             ORDER BY p.FECHA_PEDIDO DESC";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            pedidos.Add(MapearPedido(reader));
        }

        return pedidos;
    }

    public async Task<Pedido?> ObtenerPorIdAsync(int id)
    {
        const string sql = @"SELECT p.ID, p.NUMERO, p.CLIENTE_ID, p.FECHA_PEDIDO, p.FECHA_ENTREGA,
                                    p.ESTADO, p.TOTAL, p.OBSERVACIONES,
                                    c.NOMBRE AS CLIENTE_NOMBRE, c.APELLIDO AS CLIENTE_APELLIDO
                             FROM PEDIDOS p
                             JOIN CLIENTES c ON c.ID = p.CLIENTE_ID
                             WHERE p.ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", id));
        await using var reader = await cmd.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            var pedido = MapearPedido(reader);
            pedido.Detalles = (await ObtenerDetallesInternAsync(conn, id)).ToList();
            return pedido;
        }

        return null;
    }

    public async Task<int> CrearAsync(Pedido pedido)
    {
        const string sql = @"INSERT INTO PEDIDOS (NUMERO, CLIENTE_ID, FECHA_PEDIDO, FECHA_ENTREGA, ESTADO, TOTAL, OBSERVACIONES)
                             VALUES (:numero, :clienteId, :fechaPedido, :fechaEntrega, :estado, :total, :observaciones)
                             RETURNING ID INTO :newId";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("numero", pedido.Numero));
        cmd.Parameters.Add(new OracleParameter("clienteId", pedido.ClienteId));
        cmd.Parameters.Add(new OracleParameter("fechaPedido", pedido.FechaPedido));
        cmd.Parameters.Add(new OracleParameter("fechaEntrega", (object?)pedido.FechaEntrega ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("estado", pedido.Estado));
        cmd.Parameters.Add(new OracleParameter("total", pedido.Total));
        cmd.Parameters.Add(new OracleParameter("observaciones", (object?)pedido.Observaciones ?? DBNull.Value));

        var newIdParam = new OracleParameter("newId", OracleDbType.Int32)
        {
            Direction = System.Data.ParameterDirection.Output
        };
        cmd.Parameters.Add(newIdParam);

        await cmd.ExecuteNonQueryAsync();

        return Convert.ToInt32(newIdParam.Value);
    }

    public async Task<bool> ActualizarAsync(Pedido pedido)
    {
        const string sql = @"UPDATE PEDIDOS SET
                                NUMERO = :numero,
                                CLIENTE_ID = :clienteId,
                                FECHA_PEDIDO = :fechaPedido,
                                FECHA_ENTREGA = :fechaEntrega,
                                ESTADO = :estado,
                                TOTAL = :total,
                                OBSERVACIONES = :observaciones
                             WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("numero", pedido.Numero));
        cmd.Parameters.Add(new OracleParameter("clienteId", pedido.ClienteId));
        cmd.Parameters.Add(new OracleParameter("fechaPedido", pedido.FechaPedido));
        cmd.Parameters.Add(new OracleParameter("fechaEntrega", (object?)pedido.FechaEntrega ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("estado", pedido.Estado));
        cmd.Parameters.Add(new OracleParameter("total", pedido.Total));
        cmd.Parameters.Add(new OracleParameter("observaciones", (object?)pedido.Observaciones ?? DBNull.Value));
        cmd.Parameters.Add(new OracleParameter("id", pedido.Id));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var tx = conn.BeginTransaction();

        try
        {
            await using (var cmdDetalles = new OracleCommand("DELETE FROM DETALLE_PEDIDOS WHERE PEDIDO_ID = :id", conn))
            {
                cmdDetalles.Transaction = tx;
                cmdDetalles.Parameters.Add(new OracleParameter("id", id));
                await cmdDetalles.ExecuteNonQueryAsync();
            }

            await using (var cmdPedido = new OracleCommand("DELETE FROM PEDIDOS WHERE ID = :id", conn))
            {
                cmdPedido.Transaction = tx;
                cmdPedido.Parameters.Add(new OracleParameter("id", id));
                var rows = await cmdPedido.ExecuteNonQueryAsync();

                if (rows == 0)
                {
                    await tx.RollbackAsync();
                    return false;
                }
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<DetallePedido>> ObtenerDetallesAsync(int pedidoId)
    {
        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        return await ObtenerDetallesInternAsync(conn, pedidoId);
    }

    public async Task<bool> AgregarDetalleAsync(DetallePedido detalle)
    {
        const string sql = @"INSERT INTO DETALLE_PEDIDOS (PEDIDO_ID, HILO_ID, CANTIDAD, PRECIO_UNITARIO)
                             VALUES (:pedidoId, :hiloId, :cantidad, :precioUnitario)";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);

        cmd.Parameters.Add(new OracleParameter("pedidoId", detalle.PedidoId));
        cmd.Parameters.Add(new OracleParameter("hiloId", detalle.HiloId));
        cmd.Parameters.Add(new OracleParameter("cantidad", detalle.Cantidad));
        cmd.Parameters.Add(new OracleParameter("precioUnitario", detalle.PrecioUnitario));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public async Task<bool> EliminarDetalleAsync(int detalleId)
    {
        const string sql = "DELETE FROM DETALLE_PEDIDOS WHERE ID = :id";

        await using var conn = _context.CreateConnection();
        await conn.OpenAsync();
        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("id", detalleId));

        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    private static async Task<IEnumerable<DetallePedido>> ObtenerDetallesInternAsync(OracleConnection conn, int pedidoId)
    {
        var detalles = new List<DetallePedido>();
        const string sql = @"SELECT dp.ID, dp.PEDIDO_ID, dp.HILO_ID, dp.CANTIDAD, dp.PRECIO_UNITARIO,
                                    h.NOMBRE AS HILO_NOMBRE, h.CODIGO AS HILO_CODIGO
                             FROM DETALLE_PEDIDOS dp
                             JOIN HILOS h ON h.ID = dp.HILO_ID
                             WHERE dp.PEDIDO_ID = :pedidoId
                             ORDER BY dp.ID";

        await using var cmd = new OracleCommand(sql, conn);
        cmd.Parameters.Add(new OracleParameter("pedidoId", pedidoId));
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            detalles.Add(new DetallePedido
            {
                Id = Convert.ToInt32(reader["ID"]),
                PedidoId = Convert.ToInt32(reader["PEDIDO_ID"]),
                HiloId = Convert.ToInt32(reader["HILO_ID"]),
                Cantidad = Convert.ToInt32(reader["CANTIDAD"]),
                PrecioUnitario = Convert.ToDecimal(reader["PRECIO_UNITARIO"]),
                Hilo = new Hilo
                {
                    Id = Convert.ToInt32(reader["HILO_ID"]),
                    Nombre = reader["HILO_NOMBRE"].ToString()!,
                    Codigo = reader["HILO_CODIGO"].ToString()!
                }
            });
        }

        return detalles;
    }

    private static Pedido MapearPedido(System.Data.Common.DbDataReader reader)
    {
        return new Pedido
        {
            Id = Convert.ToInt32(reader["ID"]),
            Numero = reader["NUMERO"].ToString()!,
            ClienteId = Convert.ToInt32(reader["CLIENTE_ID"]),
            FechaPedido = Convert.ToDateTime(reader["FECHA_PEDIDO"]),
            FechaEntrega = reader["FECHA_ENTREGA"] == DBNull.Value
                ? null
                : Convert.ToDateTime(reader["FECHA_ENTREGA"]),
            Estado = reader["ESTADO"].ToString()!,
            Total = Convert.ToDecimal(reader["TOTAL"]),
            Observaciones = reader["OBSERVACIONES"] == DBNull.Value ? null : reader["OBSERVACIONES"].ToString(),
            Cliente = new Cliente
            {
                Id = Convert.ToInt32(reader["CLIENTE_ID"]),
                Nombre = reader["CLIENTE_NOMBRE"].ToString()!,
                Apellido = reader["CLIENTE_APELLIDO"].ToString()!
            }
        };
    }
}
