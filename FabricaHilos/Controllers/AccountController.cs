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
                return RedirectToAction("Index", "RegistroPreparatoria");
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usuario, string password, bool recordarme, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(string.Empty, "Por favor ingrese el usuario y contraseña.");
                return View();
            }

            // 1. Validar contra Oracle Database
            var loginOracle = new Login(_configuration, null);
            var usuarioOracle = loginOracle.EncontrarUsuario(usuario, password);

            if (!string.IsNullOrEmpty(usuarioOracle.c_user))
            {
                var userIdentity = await _userManager.FindByNameAsync(usuario);

                if (userIdentity == null)
                {
                    userIdentity = new ApplicationUser
                    {
                        UserName = usuario,
                        Email = $"{usuario}@fabricahilos.com",
                        NombreCompleto = usuarioOracle.c_user,
                        Cargo = usuarioOracle.c_costo ?? "Usuario",
                        EmailConfirmed = true
                    };

                    // Los usuarios de Oracle se autentican contra Oracle, no necesitan
                    // contraseña en Identity. Se crea sin contraseña para evitar que
                    // las reglas de complejidad impidan guardar el usuario en la BD.
                    var createResult = await _userManager.CreateAsync(userIdentity);
                    if (createResult.Succeeded)
                        await _userManager.AddToRoleAsync(userIdentity, "Admin");
                    else
                        _logger.LogWarning("No se pudo crear usuario Identity para {Usuario}: {Errores}",
                            usuario, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
                else if (!await _userManager.IsInRoleAsync(userIdentity, "Admin"))
                {
                    await _userManager.RemoveFromRoleAsync(userIdentity, "Trabajador");
                    await _userManager.AddToRoleAsync(userIdentity, "Admin");
                }

                await _signInManager.SignInAsync(userIdentity, recordarme);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "RegistroPreparatoria");
            }

            // 2. Validar contra Identity local
            var resultado = await _signInManager.PasswordSignInAsync(usuario, password, recordarme, lockoutOnFailure: false);
            if (resultado.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                return RedirectToAction("Index", "RegistroPreparatoria");
            }

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
