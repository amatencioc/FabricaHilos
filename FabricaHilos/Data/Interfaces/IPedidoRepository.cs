using FabricaHilos.Models;

namespace FabricaHilos.Data.Interfaces;

public interface IPedidoRepository
{
    Task<IEnumerable<Pedido>> ObtenerTodosAsync();
    Task<Pedido?> ObtenerPorIdAsync(int id);
    Task<int> CrearAsync(Pedido pedido);
    Task<bool> ActualizarAsync(Pedido pedido);
    Task<bool> EliminarAsync(int id);
    Task<IEnumerable<DetallePedido>> ObtenerDetallesAsync(int pedidoId);
    Task<bool> AgregarDetalleAsync(DetallePedido detalle);
    Task<bool> EliminarDetalleAsync(int detalleId);
}
