using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Microsoft.AspNetCore.Mvc;

namespace FabricaHilos.Controllers;

public class ClientesController : Controller
{
    private readonly IClienteRepository _clienteRepository;
    private readonly ILogger<ClientesController> _logger;

    public ClientesController(IClienteRepository clienteRepository, ILogger<ClientesController> logger)
    {
        _clienteRepository = clienteRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var clientes = await _clienteRepository.ObtenerTodosAsync();
        return View(clientes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var cliente = await _clienteRepository.ObtenerPorIdAsync(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Cliente cliente)
    {
        if (await _clienteRepository.ExisteEmailAsync(cliente.Email))
        {
            ModelState.AddModelError(nameof(cliente.Email), "Ya existe un cliente con este correo electrónico.");
        }

        if (!ModelState.IsValid) return View(cliente);

        try
        {
            cliente.FechaRegistro = DateTime.Now;
            await _clienteRepository.CrearAsync(cliente);
            TempData["Exito"] = "Cliente creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el cliente.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el cliente.");
            return View(cliente);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _clienteRepository.ObtenerPorIdAsync(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Cliente cliente)
    {
        if (id != cliente.Id) return BadRequest();

        if (await _clienteRepository.ExisteEmailAsync(cliente.Email, id))
        {
            ModelState.AddModelError(nameof(cliente.Email), "Ya existe un cliente con este correo electrónico.");
        }

        if (!ModelState.IsValid) return View(cliente);

        try
        {
            await _clienteRepository.ActualizarAsync(cliente);
            TempData["Exito"] = "Cliente actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el cliente {Id}.", id);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el cliente.");
            return View(cliente);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var cliente = await _clienteRepository.ObtenerPorIdAsync(id);
        if (cliente == null) return NotFound();
        return View(cliente);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _clienteRepository.EliminarAsync(id);
            TempData["Exito"] = "Cliente eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el cliente {Id}.", id);
            TempData["Error"] = "No se puede eliminar el cliente porque tiene pedidos asociados.";
        }

        return RedirectToAction(nameof(Index));
    }
}
