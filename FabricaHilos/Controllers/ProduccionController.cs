using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabricaHilos.Data;
using FabricaHilos.Models.Produccion;

namespace FabricaHilos.Controllers
{
    [Authorize]
    public class ProduccionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProduccionController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? buscar, EstadoOrden? estado)
        {
            var query = _context.OrdenesProduccion.Include(o => o.Empleado).AsQueryable();
            if (!string.IsNullOrEmpty(buscar))
                query = query.Where(o => o.NumeroOrden.Contains(buscar) || o.TipoHilo.Contains(buscar));
            if (estado.HasValue)
                query = query.Where(o => o.Estado == estado.Value);

            ViewBag.Buscar = buscar;
            ViewBag.EstadoFiltro = estado;
            return View(await query.OrderByDescending(o => o.FechaInicio).ToListAsync());
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Empleados = await _context.Empleados.Where(e => e.Activo).OrderBy(e => e.NombreCompleto).ToListAsync();
            // Generar número de orden automático
            var ultimo = await _context.OrdenesProduccion.OrderByDescending(o => o.Id).FirstOrDefaultAsync();
            ViewBag.NumeroOrden = $"OP-{DateTime.Now.Year}-{(ultimo?.Id ?? 0) + 1:D4}";
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(OrdenProduccion model)
        {
            if (ModelState.IsValid)
            {
                _context.OrdenesProduccion.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Orden de producción creada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Empleados = await _context.Empleados.Where(e => e.Activo).OrderBy(e => e.NombreCompleto).ToListAsync();
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> Editar(int id)
        {
            var orden = await _context.OrdenesProduccion.FindAsync(id);
            if (orden == null) return NotFound();
            ViewBag.Empleados = await _context.Empleados.Where(e => e.Activo).OrderBy(e => e.NombreCompleto).ToListAsync();
            return View(orden);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, OrdenProduccion model)
        {
            if (id != model.Id) return BadRequest();
            if (ModelState.IsValid)
            {
                _context.OrdenesProduccion.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Orden de producción actualizada.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Empleados = await _context.Empleados.Where(e => e.Activo).OrderBy(e => e.NombreCompleto).ToListAsync();
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            var orden = await _context.OrdenesProduccion.FindAsync(id);
            if (orden != null)
            {
                _context.OrdenesProduccion.Remove(orden);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Orden de producción eliminada.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
