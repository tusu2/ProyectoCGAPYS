using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoCGAPYS.ViewModels;
using ProyectoCGAPYS.Models; // Asegúrate de que este sea tu namespace real

namespace ProyectoCGAPYS.Controllers
{
    // [Authorize(Roles = "Jefa")] // Descomenta para activar seguridad
    public class RecursosHumanosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public RecursosHumanosController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ---------------------------------------------------------
        // 1. LA LISTA (DASHBOARD)
        // ---------------------------------------------------------
        // GET: RecursosHumanos/Index
        public async Task<IActionResult> Index()
        {
            var usuarios = _userManager.Users.ToList();
            var listaUsuarios = new List<UsuarioListaViewModel>();

            foreach (var usuario in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(usuario);
                var rolPrincipal = roles.FirstOrDefault() ?? "Sin Rol";
                bool bloqueado = await _userManager.IsLockedOutAsync(usuario);

                listaUsuarios.Add(new UsuarioListaViewModel
                {
                    Id = usuario.Id,
                    Email = usuario.Email,
                    Telefono = usuario.PhoneNumber ?? "N/A",
                    Rol = rolPrincipal,
                    EstaBloqueado = bloqueado
                });
            }

            return View(listaUsuarios);
        }

        // ---------------------------------------------------------
        // 2. EL REGISTRO (CREAR)
        // ---------------------------------------------------------
        // GET: RecursosHumanos/Crear
        public IActionResult Crear()
        {
            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View();
        }

        // POST: RecursosHumanos/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(RegistroUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.RolSeleccionado))
                    {
                        await _userManager.AddToRoleAsync(user, model.RolSeleccionado);
                    }

                    TempData["Mensaje"] = $"Usuario {model.Email} creado correctamente.";

                    // AQUÍ ESTÁ LA RELACIÓN:
                    // Al terminar, nos manda de regreso a la lista (Index)
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new SelectList(_roleManager.Roles.ToList(), "Name", "Name");
            return View(model);
        }

        // ---------------------------------------------------------
        // 3. BLOQUEAR / DESBLOQUEAR
        // ---------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CambiarEstado(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, null); // Desbloquear
                TempData["Mensaje"] = $"Acceso reactivado para {user.Email}";
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100)); // Bloquear
                TempData["Error"] = $"Acceso bloqueado para {user.Email}";
            }

            return RedirectToAction(nameof(Index)); // Regresa a la lista
        }
    }
}