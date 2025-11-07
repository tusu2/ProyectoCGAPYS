using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace ProyectoCGAPYS.Controllers
{
    public class EstimacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Inyectamos los servicios que necesitamos
        public EstimacionesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Estimaciones/Crear
        // Esta acción prepara y muestra el formulario vacío
        [Authorize] // Solo usuarios logueados pueden crear
        public async Task<IActionResult> Crear()
        {
            // 1. Obtener el ID del usuario actual (Contratista)
            var usuarioActual = await _userManager.GetUserAsync(User);
            if (usuarioActual == null)
            {
                return Challenge(); // No debería pasar si está [Authorize]
            }

            // 2. Lógica Clave: Encontrar los proyectos ASIGNADOS a este contratista
            // Basado en tu DB (Ia.sql), la lógica es:
            // Usuario -> Contratista -> Licitacion (como Ganador) -> Proyecto
            var proyectosAsignados = await _context.Licitaciones
                .Include(l => l.ContratistaGanador) // Unir con Contratistas
                .Include(l => l.Proyecto) // Unir con Proyectos
                .Where(l => l.ContratistaGanador.UsuarioId == usuarioActual.Id) // Filtrar por el usuario actual
                .Select(l => l.Proyecto) // Seleccionar solo los proyectos
                .Distinct() // Evitar proyectos duplicados
                .ToListAsync();

            // 3. Preparar el ViewModel
            var viewModel = new EstimacionCrearViewModel
            {
                // Crear el SelectList para el DropDown
                ProyectosAsignados = new SelectList(proyectosAsignados, "Id", "NombreProyecto")
            };

            // 4. Enviar el ViewModel a la Vista
            return View(viewModel);
        }
    }
}
