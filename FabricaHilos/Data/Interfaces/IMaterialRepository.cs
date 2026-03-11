using FabricaHilos.Models;

namespace FabricaHilos.Data.Interfaces;

public interface IMaterialRepository
{
    Task<IEnumerable<Material>> ObtenerTodosAsync();
    Task<Material?> ObtenerPorIdAsync(int id);
    Task<int> CrearAsync(Material material);
    Task<bool> ActualizarAsync(Material material);
    Task<bool> EliminarAsync(int id);
}
