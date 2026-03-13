using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabricaHilos.Data;
using FabricaHilos.Models.Produccion;
using FabricaHilos.Services.Produccion;

namespace FabricaHilos.Controllers
{
    [Authorize]
    public class RegistroPreparatoriaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IRecetaService _recetaService;
        private readonly ILogger<RegistroPreparatoriaController> _logger;

        public RegistroPreparatoriaController(
            ApplicationDbContext context, 
            IRecetaService recetaService,
            ILogger<RegistroPreparatoriaController> logger)
        {
            _context = context;
            _recetaService = recetaService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? buscar, string? maquina)
        {
            // Obtener preparatorias desde Oracle con estado = '1'
            var preparatorias = await _recetaService.ObtenerPreparatoriasAsync(buscar, maquina);

            // Cruzar con registros locales para obtener el Id de SQLite (para acciones de edición).
            // Receta es opcional: si existe se cruza por CodigoReceta + FechaInicio (segundos);
            // si la fila solo tiene Lote, se cruza por Lote + FechaInicio.
            if (preparatorias.Count > 0)
            {
                var recetas = preparatorias
                    .Where(p => !string.IsNullOrEmpty(p.Receta))
                    .Select(p => p.Receta).Distinct().ToList();

                var lotesSinReceta = preparatorias
                    .Where(p => string.IsNullOrEmpty(p.Receta) && !string.IsNullOrEmpty(p.Lote))
                    .Select(p => p.Lote).Distinct().ToList();

                var locales = await _context.OrdenesProduccion
                    .Where(o => (!string.IsNullOrEmpty(o.CodigoReceta) && recetas.Contains(o.CodigoReceta))
                             || (string.IsNullOrEmpty(o.CodigoReceta) && o.Lote != null && lotesSinReceta.Contains(o.Lote)))
                    .Select(o => new { o.Id, o.CodigoReceta, o.Lote, o.FechaInicio })
                    .ToListAsync();

                foreach (var p in preparatorias)
                {
                    var fechaStr = p.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss");

                    if (!string.IsNullOrEmpty(p.Receta))
                    {
                        // Fila con receta: cruzar por CodigoReceta + FechaInicio
                        p.LocalId = locales.FirstOrDefault(l =>
                            l.CodigoReceta == p.Receta &&
                            l.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss") == fechaStr)?.Id;
                    }
                    else
                    {
                        // Fila sin receta: cruzar por Lote + FechaInicio
                        p.LocalId = locales.FirstOrDefault(l =>
                            string.IsNullOrEmpty(l.CodigoReceta) &&
                            l.Lote == p.Lote &&
                            l.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss") == fechaStr)?.Id;
                    }
                }

                // Crear registro local para preparatorias de Oracle sin contraparte en SQLite
                foreach (var p in preparatorias.Where(p => !p.LocalId.HasValue).ToList())
                {
                    try
                    {
                        var nuevaOrden = new OrdenProduccion
                        {
                            CodigoReceta = string.IsNullOrEmpty(p.Receta) ? null : p.Receta,
                            Lote = string.IsNullOrEmpty(p.Lote) ? "-" : p.Lote,
                            DescripcionMaterial = string.IsNullOrEmpty(p.Material) ? "-" : p.Material,
                            CodigoMaquina = string.IsNullOrEmpty(p.TipoMaquina) ? "-" : p.TipoMaquina,
                            Maquina = string.IsNullOrEmpty(p.CodigoMaquina) ? "-" : p.CodigoMaquina,
                            Titulo = string.IsNullOrEmpty(p.Titulo) ? "-" : p.Titulo,
                            FechaInicio = p.FechaInicio,
                            EmpleadoId = string.IsNullOrEmpty(p.CodigoOperario) ? "-" : p.CodigoOperario,
                            Turno = string.IsNullOrEmpty(p.Turno) ? "-" : p.Turno,
                            PasoManuar = string.IsNullOrEmpty(p.PasoManual) ? "-" : p.PasoManual,
                            Estado = EstadoOrden.EnProceso,
                            Cerrado = false
                        };
                        _context.OrdenesProduccion.Add(nuevaOrden);
                        await _context.SaveChangesAsync();
                        p.LocalId = nuevaOrden.Id;
                        _logger.LogInformation("Registro local creado para Oracle: Receta={Receta}, Lote={Lote}, FechaInicio={FechaInicio}, Id={Id}",
                            p.Receta, p.Lote, p.FechaInicio, nuevaOrden.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "No se pudo crear registro local para Oracle: Receta={Receta}, Lote={Lote}", p.Receta, p.Lote);
                    }
                }
            }

            ViewBag.Buscar = buscar;
            ViewBag.MaquinaFiltro = maquina;

            // Obtener lista de máquinas únicas desde Oracle para el combo (código + descripción)
            var maquinasUnicas = preparatorias
                .Where(p => !string.IsNullOrEmpty(p.CodigoMaquina))
                .GroupBy(p => p.CodigoMaquina)
                .Select(g => new
                {
                    Codigo = g.Key,
                    Descripcion = string.IsNullOrEmpty(g.First().DescripcionMaquina)
                        ? g.Key
                        : g.First().DescripcionMaquina
                })
                .OrderBy(m => m.Descripcion)
                .ToList();

            ViewBag.Maquinas = maquinasUnicas;

            return View(preparatorias);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> Crear()
        {
            ViewBag.Empleados = await _recetaService.ObtenerEmpleadosAsync();
            ViewBag.TiposMaquinas = await _recetaService.ObtenerTiposMaquinasAsync();
            ViewBag.Titulos = await _recetaService.ObtenerTitulosAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(OrdenProduccion model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Establecer valores por defecto
                    model.Estado = EstadoOrden.Pendiente;
                    model.Cerrado = false;

                    // Asegurar que la fecha de inicio sea la actual
                    if (model.FechaInicio == default(DateTime) || model.FechaInicio == DateTime.MinValue)
                    {
                        model.FechaInicio = DateTime.Now;
                    }

                    _logger.LogInformation("Creando preparatoria: Receta={Receta}, Lote={Lote}, FechaInicio={FechaInicio}", 
                        model.CodigoReceta, model.Lote, model.FechaInicio);

                    // Guardar en base de datos local (SQLite)
                    _context.OrdenesProduccion.Add(model);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Preparatoria guardada en SQLite con Id={Id}", model.Id);

                    // Insertar en Oracle (H_RPRODUC)
                    var insertadoEnOracle = await _recetaService.InsertarPreparatoriaAsync(model);

                    if (insertadoEnOracle)
                    {
                        TempData["Success"] = "Preparatoria creada exitosamente y registrada en Oracle.";
                        _logger.LogInformation("Preparatoria {Id} creada y registrada en Oracle exitosamente", model.Id);
                    }
                    else
                    {
                        TempData["Warning"] = "Preparatoria creada, pero no se pudo registrar en Oracle. Revise los logs.";
                        _logger.LogWarning("Preparatoria {Id} creada pero falló el registro en Oracle", model.Id);
                    }

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al crear preparatoria. InnerException: {InnerException}", 
                        ex.InnerException?.Message ?? "N/A");

                    var errorMsg = ex.InnerException?.Message ?? ex.Message;
                    TempData["Error"] = $"Error al crear la preparatoria: {errorMsg}";
                }
            }
            else
            {
                _logger.LogWarning("ModelState inválido al crear preparatoria. Errores: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            ViewBag.Empleados = await _recetaService.ObtenerEmpleadosAsync();
            ViewBag.TiposMaquinas = await _recetaService.ObtenerTiposMaquinasAsync();
            ViewBag.Titulos = await _recetaService.ObtenerTitulosAsync();
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> Editar(int id)
        {
            var orden = await _context.OrdenesProduccion.FindAsync(id);
            if (orden == null)
            {
                TempData["Error"] = "Preparatoria no encontrada.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Empleados = await _recetaService.ObtenerEmpleadosAsync();
            ViewBag.TiposMaquinas = await _recetaService.ObtenerTiposMaquinasAsync();
            ViewBag.Titulos = await _recetaService.ObtenerTitulosAsync();
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
                var orden = await _context.OrdenesProduccion.FindAsync(id);
                if (orden == null)
                {
                    TempData["Error"] = "Preparatoria no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                // Guardar valores originales ANTES de modificar (se usan en el WHERE del Oracle UPDATE)
                var oldReceta    = orden.CodigoReceta;
                var oldLote      = orden.Lote;
                var oldTpMaq     = orden.CodigoMaquina;
                var oldCodMaq    = orden.Maquina;
                var oldTitulo    = orden.Titulo;

                // Actualizar solo los campos editables. Estado, Cerrado y FechaInicio no se modifican.
                orden.CodigoReceta        = model.CodigoReceta;
                orden.Lote                = model.Lote;
                orden.DescripcionMaterial = model.DescripcionMaterial;
                orden.CodigoMaquina       = model.CodigoMaquina;
                orden.Maquina             = model.Maquina;
                orden.Titulo              = model.Titulo;
                orden.EmpleadoId          = model.EmpleadoId;
                orden.Turno               = model.Turno;
                orden.PasoManuar          = model.PasoManuar;

                await _context.SaveChangesAsync();

                // UPDATE en Oracle (H_RPRODUC). FECHA_INI y ESTADO no se modifican.
                var actualizadoEnOracle = await _recetaService.ActualizarPreparatoriaOracleAsync(
                    oldReceta, oldLote, oldTpMaq, oldCodMaq, oldTitulo, orden.FechaInicio,
                    orden.CodigoReceta, orden.Lote, orden.CodigoMaquina, orden.Maquina, orden.Titulo,
                    orden.EmpleadoId, orden.Turno, orden.PasoManuar);

                if (actualizadoEnOracle)
                {
                    TempData["Success"] = "Preparatoria actualizada exitosamente en Oracle y sistema local.";
                    _logger.LogInformation("Preparatoria {Id} actualizada en Oracle y SQLite.", id);
                }
                else
                {
                    TempData["Warning"] = "Preparatoria actualizada localmente, pero no se pudo actualizar en Oracle.";
                    _logger.LogWarning("Preparatoria {Id} actualizada en SQLite pero falló en Oracle.", id);
                }

                return RedirectToAction(nameof(Index));
            }
            ViewBag.Empleados = await _recetaService.ObtenerEmpleadosAsync();
            ViewBag.TiposMaquinas = await _recetaService.ObtenerTiposMaquinasAsync();
            ViewBag.Titulos = await _recetaService.ObtenerTitulosAsync();
            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id)
        {
            try
            {
                var orden = await _context.OrdenesProduccion.FindAsync(id);
                if (orden == null)
                {
                    TempData["Error"] = "Preparatoria no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                if (orden.Estado == EstadoOrden.Cancelada)
                {
                    TempData["Warning"] = "La preparatoria ya se encuentra anulada.";
                    return RedirectToAction(nameof(Index));
                }

                // Actualizar estado local
                orden.Estado = EstadoOrden.Cancelada;
                _context.OrdenesProduccion.Update(orden);
                await _context.SaveChangesAsync();

                // Actualizar ESTADO = '9' en Oracle (H_RPRODUC) usando todos los campos clave
                var anulado = await _recetaService.AnularPreparatoriaOracleAsync(
                    orden.CodigoReceta,
                    orden.Lote,
                    orden.CodigoMaquina,
                    orden.Maquina,
                    orden.Titulo,
                    orden.FechaInicio);

                if (anulado)
                {
                    TempData["Success"] = $"Preparatoria {orden.CodigoReceta} anulada exitosamente.";
                    _logger.LogInformation("Preparatoria {CodigoReceta} anulada en Oracle y SQLite.", orden.CodigoReceta);
                }
                else
                {
                    TempData["Warning"] = $"Preparatoria {orden.CodigoReceta} anulada localmente, pero no se pudo actualizar en Oracle.";
                    _logger.LogWarning("Preparatoria {CodigoReceta} anulada en SQLite pero falló en Oracle.", orden.CodigoReceta);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al anular preparatoria con Id={Id}", id);
                TempData["Error"] = $"Error al anular la preparatoria: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CerrarPreparatoria(int id)
        {
            try
            {
                var orden = await _context.OrdenesProduccion.FindAsync(id);
                if (orden == null)
                {
                    TempData["Error"] = "Preparatoria no encontrada.";
                    return RedirectToAction(nameof(Index));
                }

                if (orden.Cerrado)
                {
                    TempData["Warning"] = "La preparatoria ya está cerrada.";
                    return RedirectToAction(nameof(Index));
                }

                orden.Cerrado = true;
                orden.Estado = EstadoOrden.Completada;
                _context.OrdenesProduccion.Update(orden);
                await _context.SaveChangesAsync();

                // Actualizar ESTADO = '3' en Oracle (H_RPRODUC)
                var cerrado = await _recetaService.CerrarPreparatoriaOracleAsync(
                    orden.CodigoReceta,
                    orden.Lote,
                    orden.CodigoMaquina,
                    orden.Maquina,
                    orden.Titulo,
                    orden.FechaInicio);

                if (cerrado)
                {
                    TempData["Success"] = $"Preparatoria {orden.CodigoReceta} cerrada exitosamente.";
                    _logger.LogInformation("Preparatoria {CodigoReceta} cerrada en Oracle y SQLite.", orden.CodigoReceta);
                }
                else
                {
                    TempData["Warning"] = $"Preparatoria {orden.CodigoReceta} cerrada localmente, pero no se pudo actualizar en Oracle.";
                    _logger.LogWarning("Preparatoria {CodigoReceta} cerrada en SQLite pero falló en Oracle.", orden.CodigoReceta);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar preparatoria con Id={Id}", id);
                TempData["Error"] = $"Error al cerrar la preparatoria: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> DetalleProduccion(int id)
        {
            var orden = await _context.OrdenesProduccion
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orden == null)
            {
                TempData["Error"] = "Preparatoria no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            return View(orden);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DetalleProduccion(int id, decimal? velocidad, decimal? metraje, int? rolloTacho, decimal? kgNeto)
        {
            var orden = await _context.OrdenesProduccion.FindAsync(id);
            if (orden == null)
            {
                TempData["Error"] = "Preparatoria no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            if (!velocidad.HasValue || !metraje.HasValue || !kgNeto.HasValue)
            {
                TempData["Error"] = "Los campos Velocidad, Metraje y Kg Neto son obligatorios.";
                return View(orden);
            }

            // Actualizar solo los campos de detalle de producción
            orden.Velocidad = velocidad;
            orden.Metraje = metraje;
            orden.RolloTacho = rolloTacho;
            orden.KgNeto = kgNeto;

            _context.OrdenesProduccion.Update(orden);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Detalle de producción actualizado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// API para buscar receta en Oracle por código
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> BuscarReceta(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return Json(new { success = false, message = "Código requerido" });
            }

            try
            {
                _logger.LogInformation("API BuscarReceta llamada con código: {Codigo}", codigo);

                var receta = await _recetaService.BuscarRecetaPorCodigoAsync(codigo);

                if (receta != null)
                {
                    _logger.LogInformation("Receta encontrada, retornando datos");
                    return Json(new 
                    { 
                        success = true, 
                        data = new 
                        {
                            numero = receta.Numero,
                            lote = receta.Lote,
                            material = receta.Material
                        }
                    });
                }

                _logger.LogWarning("Receta no encontrada para código: {Codigo}", codigo);
                return Json(new { success = false, message = "No se encontró la receta" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en API BuscarReceta: {Codigo}", codigo);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// API para buscar lote en Oracle por código
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> BuscarLote(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return Json(new { success = false, message = "Código requerido" });
            }

            try
            {
                _logger.LogInformation("API BuscarLote llamada con código: {Codigo}", codigo);

                var lote = await _recetaService.BuscarLotePorCodigoAsync(codigo);

                if (lote != null)
                {
                    _logger.LogInformation("Lote encontrado, retornando datos");
                    return Json(new 
                    { 
                        success = true, 
                        data = new 
                        {
                            lote = lote.Lote,
                            receta = lote.Receta,
                            material = lote.Material
                        }
                    });
                }

                _logger.LogWarning("Lote no encontrado para código: {Codigo}", codigo);
                return Json(new { success = false, message = "No se encontró el lote" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en API BuscarLote: {Codigo}", codigo);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// API para obtener máquinas por tipo desde Oracle
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> ObtenerMaquinasPorTipo(string tipoMaquina)
        {
            if (string.IsNullOrWhiteSpace(tipoMaquina))
            {
                return Json(new { success = false, message = "Tipo de máquina requerido" });
            }

            try
            {
                _logger.LogInformation("API ObtenerMaquinasPorTipo llamada con tipo: {TipoMaquina}", tipoMaquina);

                var maquinas = await _recetaService.ObtenerMaquinasPorTipoAsync(tipoMaquina);

                _logger.LogInformation("Se obtuvieron {Count} máquinas", maquinas.Count);
                return Json(new 
                { 
                    success = true, 
                    data = maquinas.Select(m => new 
                    {
                        codigo = m.CodigoMaquina,
                        descripcion = m.DescripcionMaquina,
                        textoCompleto = m.TextoCompleto
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en API ObtenerMaquinasPorTipo: {TipoMaquina}", tipoMaquina);
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// API para obtener el PESO de un título desde H_TITULOS (usado en cálculo de Kg Neto)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia,Supervisor")]
        public async Task<IActionResult> ObtenerPesoTitulo(string titulo)
        {
            try
            {
                var peso = string.IsNullOrWhiteSpace(titulo)
                    ? 0m
                    : await _recetaService.ObtenerPesoTituloAsync(titulo);

                return Json(new { success = true, peso });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en API ObtenerPesoTitulo: {Titulo}", titulo);
                return Json(new { success = false, peso = 0m });
            }
        }
    }
}
