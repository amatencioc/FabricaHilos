using FabricaHilos.Data.Interfaces;
using FabricaHilos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FabricaHilos.Controllers;

public class PedidosController : Controller
{
    private readonly IPedidoRepository _pedidoRepository;
    private readonly IClienteRepository _clienteRepository;
    private readonly IHiloRepository _hiloRepository;
    private readonly ILogger<PedidosController> _logger;

    public PedidosController(
        IPedidoRepository pedidoRepository,
        IClienteRepository clienteRepository,
        IHiloRepository hiloRepository,
        ILogger<PedidosController> logger)
    {
        _pedidoRepository = pedidoRepository;
        _clienteRepository = clienteRepository;
        _hiloRepository = hiloRepository;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var pedidos = await _pedidoRepository.ObtenerTodosAsync();
        return View(pedidos);
    }

    public async Task<IActionResult> Details(int id)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id);
        if (pedido == null) return NotFound();

        var hilos = await _hiloRepository.ObtenerTodosAsync();
        ViewBag.Hilos = new SelectList(
            hilos.Where(h => h.Activo),
            "Id",
            "Nombre");

        return View(pedido);
    }

    public async Task<IActionResult> Create()
    {
        await CargarSelectListsAsync();
        var pedido = new Pedido
        {
            FechaPedido = DateTime.Now,
            Estado = EstadoPedido.Pendiente
        };
        return View(pedido);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pedido pedido)
    {
        if (!ModelState.IsValid)
        {
            await CargarSelectListsAsync();
            return View(pedido);
        }

        try
        {
            var id = await _pedidoRepository.CrearAsync(pedido);
            TempData["Exito"] = $"Pedido {pedido.Numero} creado exitosamente.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear el pedido.");
            ModelState.AddModelError(string.Empty, "Ocurrió un error al guardar el pedido.");
            await CargarSelectListsAsync();
            return View(pedido);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id);
        if (pedido == null) return NotFound();
        await CargarSelectListsAsync(pedido.ClienteId);
        return View(pedido);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Pedido pedido)
    {
        if (id != pedido.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await CargarSelectListsAsync(pedido.ClienteId);
            return View(pedido);
        }

        try
        {
            await _pedidoRepository.ActualizarAsync(pedido);
            TempData["Exito"] = "Pedido actualizado exitosamente.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el pedido {Id}.", id);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el pedido.");
            await CargarSelectListsAsync(pedido.ClienteId);
            return View(pedido);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var pedido = await _pedidoRepository.ObtenerPorIdAsync(id);
        if (pedido == null) return NotFound();
        return View(pedido);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _pedidoRepository.EliminarAsync(id);
            TempData["Exito"] = "Pedido eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el pedido {Id}.", id);
            TempData["Error"] = "No se puede eliminar el pedido.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarDetalle(int pedidoId, int hiloId, int cantidad, decimal precioUnitario)
    {
        try
        {
            var detalle = new DetallePedido
            {
                PedidoId = pedidoId,
                HiloId = hiloId,
                Cantidad = cantidad,
                PrecioUnitario = precioUnitario
            };

            await _pedidoRepository.AgregarDetalleAsync(detalle);

            var total = (await _pedidoRepository.ObtenerDetallesAsync(pedidoId))
                .Sum(d => d.Subtotal);

            var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId);
            if (pedido != null)
            {
                pedido.Total = total;
                await _pedidoRepository.ActualizarAsync(pedido);
            }

            TempData["Exito"] = "Detalle agregado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al agregar detalle al pedido {PedidoId}.", pedidoId);
            TempData["Error"] = "Ocurrió un error al agregar el detalle.";
        }

        return RedirectToAction(nameof(Details), new { id = pedidoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarDetalle(int detalleId, int pedidoId)
    {
        try
        {
            await _pedidoRepository.EliminarDetalleAsync(detalleId);

            var total = (await _pedidoRepository.ObtenerDetallesAsync(pedidoId))
                .Sum(d => d.Subtotal);

            var pedido = await _pedidoRepository.ObtenerPorIdAsync(pedidoId);
            if (pedido != null)
            {
                pedido.Total = total;
                await _pedidoRepository.ActualizarAsync(pedido);
            }

            TempData["Exito"] = "Detalle eliminado exitosamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar el detalle {DetalleId}.", detalleId);
            TempData["Error"] = "No se puede eliminar el detalle.";
        }

        return RedirectToAction(nameof(Details), new { id = pedidoId });
    }

    private async Task CargarSelectListsAsync(int? clienteSeleccionado = null)
    {
        var clientes = await _clienteRepository.ObtenerTodosAsync();
        ViewBag.ClienteId = new SelectList(
            clientes.Where(c => c.Activo),
            "Id",
            "NombreCompleto",
            clienteSeleccionado);

        var hilos = await _hiloRepository.ObtenerTodosAsync();
        ViewBag.Hilos = new SelectList(
            hilos.Where(h => h.Activo),
            "Id",
            "Nombre");

        ViewBag.Estados = new SelectList(EstadoPedido.Todos);
    }
}
