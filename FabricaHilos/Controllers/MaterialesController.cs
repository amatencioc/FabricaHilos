using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Microsoft.AspNetCore.Mvc;

namespace FabricaHilos.Controllers;

public class MaterialesController : Controller
{
    private readonly IMaterialRepository _materialRepository;
    private readonly ILogger<MaterialesController> _logger;

    public MaterialesController(IMaterialRepository materialRepository, ILogger<MaterialesController> logger)
    {
        _materialRepository = materialRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var materiales = await _materialRepository.ObtenerTodosAsync();
        return View(materiales);
    }

    public async Task<IActionResult> Details(int id)
    {
        var material = await _materialRepository.ObtenerPorIdAsync(id);
        if (material == null) return NotFound();
        return View(material);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Material material)
    {
        if (!ModelState.IsValid) return View(material);

        try
        {
            await _materialRepository.CrearAsync(material);
            TempData["Exito"] = "Material creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el material.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el material.");
            return View(material);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var material = await _materialRepository.ObtenerPorIdAsync(id);
        if (material == null) return NotFound();
        return View(material);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Material material)
    {
        if (id != material.Id) return BadRequest();
        if (!ModelState.IsValid) return View(material);

        try
        {
            await _materialRepository.ActualizarAsync(material);
            TempData["Exito"] = "Material actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el material {Id}.", id);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el material.");
            return View(material);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var material = await _materialRepository.ObtenerPorIdAsync(id);
        if (material == null) return NotFound();
        return View(material);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _materialRepository.EliminarAsync(id);
            TempData["Exito"] = "Material eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el material {Id}.", id);
            TempData["Error"] = "No se puede eliminar el material.";
        }

        return RedirectToAction(nameof(Index));
    }
}
