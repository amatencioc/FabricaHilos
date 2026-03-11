using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Microsoft.AspNetCore.Mvc;

namespace FabricaHilos.Controllers;

public class HilosController : Controller
{
    private readonly IHiloRepository _hiloRepository;
    private readonly ILogger<HilosController> _logger;

    public HilosController(IHiloRepository hiloRepository, ILogger<HilosController> logger)
    {
        _hiloRepository = hiloRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var hilos = await _hiloRepository.ObtenerTodosAsync();
        return View(hilos);
    }

    public async Task<IActionResult> Details(int id)
    {
        var hilo = await _hiloRepository.ObtenerPorIdAsync(id);
        if (hilo == null) return NotFound();
        return View(hilo);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Hilo hilo)
    {
        if (await _hiloRepository.ExisteCodigoAsync(hilo.Codigo))
        {
            ModelState.AddModelError(nameof(hilo.Codigo), "Ya existe un hilo con este código.");
        }

        if (!ModelState.IsValid) return View(hilo);

        try
        {
            hilo.FechaCreacion = DateTime.Now;
            await _hiloRepository.CrearAsync(hilo);
            TempData["Exito"] = "Hilo creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el hilo.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el hilo.");
            return View(hilo);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var hilo = await _hiloRepository.ObtenerPorIdAsync(id);
        if (hilo == null) return NotFound();
        return View(hilo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Hilo hilo)
    {
        if (id != hilo.Id) return BadRequest();

        if (await _hiloRepository.ExisteCodigoAsync(hilo.Codigo, id))
        {
            ModelState.AddModelError(nameof(hilo.Codigo), "Ya existe un hilo con este código.");
        }

        if (!ModelState.IsValid) return View(hilo);

        try
        {
            await _hiloRepository.ActualizarAsync(hilo);
            TempData["Exito"] = "Hilo actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el hilo {Id}.", id);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el hilo.");
            return View(hilo);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var hilo = await _hiloRepository.ObtenerPorIdAsync(id);
        if (hilo == null) return NotFound();
        return View(hilo);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _hiloRepository.EliminarAsync(id);
            TempData["Exito"] = "Hilo eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el hilo {Id}.", id);
            TempData["Error"] = "No se puede eliminar el hilo porque tiene pedidos asociados.";
        }

        return RedirectToAction(nameof(Index));
    }
}
