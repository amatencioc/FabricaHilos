using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FabricaHilos.Models;
using FabricaHilos.Logica;

namespace FabricaHilos.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// Endpoint de diagnóstico para probar conexión a Oracle y validación de usuarios
        /// Acceso: https://localhost:7777/Account/TestOracle?usuario=VENTAS7&password=460910
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestOracle(string usuario = "VENTAS7", string password = "460910")
        {
            var html = new System.Text.StringBuilder();

            try
            {
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang='es'>");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset='utf-8' />");
                html.AppendLine("    <title>Diagnóstico Oracle</title>");
                html.AppendLine("    <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet' />");
                html.AppendLine("</head>");
                html.AppendLine("<body style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); min-height: 100vh; padding: 20px;'>");
                html.AppendLine("    <div class='container'>");
                html.AppendLine("        <div class='card mt-4'>");
                html.AppendLine("            <div class='card-header bg-primary text-white'>");
                html.AppendLine("                <h3>Diagnóstico Oracle</h3>");
                html.AppendLine("            </div>");
                html.AppendLine("            <div class='card-body'>");

                html.AppendLine($"                <p><strong>Usuario:</strong> {usuario}</p>");
                html.AppendLine($"                <p><strong>Contraseña:</strong> {new string('*', password?.Length ?? 0)}</p>");
                html.AppendLine("                <hr>");

                // Configuración
                var connectionString = _configuration.GetConnectionString("OracleConnection");
                html.AppendLine($"                <h5>1. Configuración</h5>");
                if (string.IsNullOrEmpty(connectionString))
                {
                    html.AppendLine("                <div class='alert alert-danger'>❌ Cadena de conexión no encontrada</div>");
                    html.AppendLine("            </div></div></div></body></html>");
                    return Content(html.ToString(), "text/html");
                }
                html.AppendLine($"                <div class='alert alert-success'>✅ Cadena de conexión OK</div>");
                html.AppendLine($"                <pre>{connectionString}</pre>");

                // Prueba de conexión
                html.AppendLine($"                <h5>2. Prueba de Conexión</h5>");

                var loginOracle = new Login(_configuration, null);
                var inicio = DateTime.Now;
                var usuarioOracle = loginOracle.EncontrarUsuario(usuario, password);
                var tiempoRespuesta = (DateTime.Now - inicio).TotalMilliseconds;

                html.AppendLine($"                <p>Tiempo de respuesta: {tiempoRespuesta:F2} ms</p>");

                if (!string.IsNullOrEmpty(usuarioOracle.c_user))
                {
                    html.AppendLine("                <div class='alert alert-success'>");
                    html.AppendLine("                    <h5>✅ USUARIO ENCONTRADO</h5>");
                    html.AppendLine("                    <table class='table'>");
                    html.AppendLine("                        <tr><td>Usuario (c_user)</td><td>" + usuarioOracle.c_user + "</td></tr>");
                    html.AppendLine("                        <tr><td>Centro de Costo (c_costo)</td><td>" + (usuarioOracle.c_costo ?? "Sin centro de costo") + "</td></tr>");
                    html.AppendLine("                    </table>");
                    html.AppendLine("                </div>");
                    html.AppendLine("                <p><strong>✅ El usuario existe en Oracle y funciona correctamente.</strong></p>");
                    html.AppendLine("                <a href='/Account/Login' class='btn btn-success'>Ir al Login</a>");
                }
                else
                {
                    html.AppendLine("                <div class='alert alert-warning'>");
                    html.AppendLine("                    <h5>❌ USUARIO NO ENCONTRADO</h5>");
                    html.AppendLine("                    <p>La conexión a Oracle funciona pero el usuario no existe o la contraseña es incorrecta.</p>");
                    html.AppendLine("                    <p><strong>Posibles causas:</strong></p>");
                    html.AppendLine("                    <ul>");
                    html.AppendLine($"                        <li>El usuario '{usuario}' no existe en la tabla cs_user</li>");
                    html.AppendLine($"                        <li>La contraseña '{password}' es incorrecta</li>");
                    html.AppendLine("                        <li>Hay diferencias de mayúsculas/minúsculas</li>");
                    html.AppendLine("                    </ul>");
                    html.AppendLine("                </div>");
                }

                html.AppendLine("            </div>");
                html.AppendLine("        </div>");
                html.AppendLine("    </div>");
                html.AppendLine("</body>");
                html.AppendLine("</html>");
            }
            catch (Exception ex)
            {
                html.Clear();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html><head><title>Error</title>");
                html.AppendLine("<link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css' rel='stylesheet' /></head>");
                html.AppendLine("<body style='padding: 20px;'>");
                html.AppendLine("<div class='container'><div class='alert alert-danger'>");
                html.AppendLine("<h4>❌ ERROR</h4>");
                html.AppendLine($"<p><strong>Tipo:</strong> {ex.GetType().Name}</p>");
                html.AppendLine($"<p><strong>Mensaje:</strong> {System.Net.WebUtility.HtmlEncode(ex.Message)}</p>");
                html.AppendLine($"<details><summary>Stack Trace</summary><pre>{System.Net.WebUtility.HtmlEncode(ex.StackTrace)}</pre></details>");
                html.AppendLine("</div></div></body></html>");
            }

            return Content(html.ToString(), "text/html");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usuario, string password, bool recordarme, string? returnUrl = null)
        {
            // LOGS EN CONSOLA (siempre aparecen)
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine("🔐 INICIO DE PROCESO DE LOGIN");
            Console.WriteLine("═══════════════════════════════════════════════════");
            Console.WriteLine($"👤 Usuario ingresado: {usuario}");
            Console.WriteLine($"🔑 Contraseña ingresada: {new string('*', password?.Length ?? 0)}");

            _logger.LogInformation("═══════════════════════════════════════════════════");
            _logger.LogInformation("🔐 INICIO DE PROCESO DE LOGIN");
            _logger.LogInformation("═══════════════════════════════════════════════════");
            _logger.LogInformation("👤 Usuario ingresado: {Usuario}", usuario);
            _logger.LogInformation("🔑 Contraseña ingresada: {Password}", password != null ? new string('*', password.Length) : "null");

            ViewData["ReturnUrl"] = returnUrl;
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("❌ Usuario o contraseña vacíos");
                _logger.LogWarning("❌ Usuario o contraseña vacíos");
                ModelState.AddModelError(string.Empty, "Por favor ingrese el usuario y contraseña.");
                return View();
            }

            // ============================================================
            // VALIDACIÓN HÍBRIDA: Primero valida en Oracle, luego en Identity
            // ============================================================

            // 1. Intentar validar contra Oracle Database
            Console.WriteLine("🔄 Paso 1: Validando contra Oracle Database...");
            _logger.LogInformation("🔄 Paso 1: Validando contra Oracle Database...");
            var loggerFactory = HttpContext.RequestServices.GetService<ILoggerFactory>();
            var loginLogger = loggerFactory?.CreateLogger<Login>();
            var loginOracle = new Login(_configuration, loginLogger);
            var usuarioOracle = loginOracle.EncontrarUsuario(usuario, password);

            if (!string.IsNullOrEmpty(usuarioOracle.c_user))
            {
                // Usuario válido en Oracle
                _logger.LogInformation("Usuario {Usuario} validado exitosamente en Oracle", usuarioOracle.c_user);

                // Pasar datos de Oracle a TempData para mostrar en consola del navegador
                TempData["OracleUser"] = usuarioOracle.c_user;
                TempData["OracleCentroCosto"] = usuarioOracle.c_costo ?? "Sin centro de costo";
                TempData["OracleLoginSuccess"] = "true";

                // Buscar o crear usuario en Identity para mantener sesión
                var userIdentity = await _userManager.FindByNameAsync(usuario);

                if (userIdentity == null)
                {
                    // Crear usuario en Identity si no existe
                    userIdentity = new ApplicationUser
                    {
                        UserName = usuario,
                        Email = $"{usuario}@fabricahilos.com",
                        NombreCompleto = usuarioOracle.c_user,
                        Cargo = usuarioOracle.c_costo ?? "Usuario",
                        EmailConfirmed = true
                    };

                    var createResult = await _userManager.CreateAsync(userIdentity, password);
                    if (!createResult.Succeeded)
                    {
                        _logger.LogWarning("❌ No se pudo crear usuario en Identity para {Usuario}", usuario);
                        _logger.LogWarning("   Errores: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        // Asignar rol por defecto
                        await _userManager.AddToRoleAsync(userIdentity, "Trabajador");
                        _logger.LogInformation("✅ Usuario {Usuario} creado en Identity desde Oracle", usuario);
                    }
                }
                else
                {
                    _logger.LogInformation("ℹ️ Usuario {Usuario} ya existe en Identity, usando existente", usuario);
                }

                // Iniciar sesión con Identity
                _logger.LogInformation("🔄 Iniciando sesión con Identity...");
                await _signInManager.SignInAsync(userIdentity, recordarme);
                _logger.LogInformation("✅ Usuario {Usuario} inició sesión exitosamente (validado por Oracle)", usuario);
                _logger.LogInformation("═══════════════════════════════════════════════════");

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            // 2. Si no se encuentra en Oracle, intentar con Identity (usuarios locales)
            _logger.LogInformation("🔄 Paso 2: Usuario no encontrado en Oracle, validando contra Identity local...");
            var resultado = await _signInManager.PasswordSignInAsync(usuario, password, recordarme, lockoutOnFailure: false);
            if (resultado.Succeeded)
            {
                _logger.LogInformation("✅ Usuario {Usuario} inició sesión (Identity local)", usuario);
                _logger.LogInformation("═══════════════════════════════════════════════════");
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }

            _logger.LogWarning("❌ LOGIN FALLIDO para usuario {Usuario}", usuario);
            _logger.LogWarning("   - No existe en Oracle");
            _logger.LogWarning("   - No existe en Identity local");
            _logger.LogInformation("═══════════════════════════════════════════════════");
            ModelState.AddModelError(string.Empty, "Usuario o contraseña incorrectos.");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Gerencia")]
        public async Task<IActionResult> Register()
        {
            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Gerencia")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string nombreCompleto, string email, string password, string rol)
        {
            if (string.IsNullOrEmpty(nombreCompleto) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Todos los campos son obligatorios.");
                ViewBag.Roles = await _roleManager.Roles.ToListAsync();
                return View();
            }

            var usuario = new ApplicationUser
            {
                UserName = email,
                Email = email,
                NombreCompleto = nombreCompleto,
                EmailConfirmed = true
            };

            var resultado = await _userManager.CreateAsync(usuario, password);
            if (resultado.Succeeded)
            {
                if (!string.IsNullOrEmpty(rol) && await _roleManager.RoleExistsAsync(rol))
                    await _userManager.AddToRoleAsync(usuario, rol);

                TempData["Success"] = $"Usuario {email} creado exitosamente.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in resultado.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            ViewBag.Roles = await _roleManager.Roles.ToListAsync();
            return View();
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}
