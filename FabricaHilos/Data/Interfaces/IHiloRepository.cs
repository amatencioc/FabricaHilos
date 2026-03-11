using FabricaHilos.Models;

namespace FabricaHilos.Data.Interfaces;

public interface IHiloRepository
{
    Task<IEnumerable<Hilo>> ObtenerTodosAsync();
    Task<Hilo?> ObtenerPorIdAsync(int id);
    Task<int> CrearAsync(Hilo hilo);
    Task<bool> ActualizarAsync(Hilo hilo);
    Task<bool> EliminarAsync(int id);
    Task<bool> ExisteCodigoAsync(string codigo, int? idExcluir = null);
}
