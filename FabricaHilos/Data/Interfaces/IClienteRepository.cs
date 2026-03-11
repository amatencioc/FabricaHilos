using FabricaHilos.Models;

namespace FabricaHilos.Data.Interfaces;

public interface IClienteRepository
{
    Task<IEnumerable<Cliente>> ObtenerTodosAsync();
    Task<Cliente?> ObtenerPorIdAsync(int id);
    Task<int> CrearAsync(Cliente cliente);
    Task<bool> ActualizarAsync(Cliente cliente);
    Task<bool> EliminarAsync(int id);
    Task<bool> ExisteEmailAsync(string email, int? idExcluir = null);
}
