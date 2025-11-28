using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Data;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoCGAPYS.Controllers
{
    // [Authorize(Roles = "Jefa,Empleado1")] // Descomenta si necesitas seguridad
    public class ContratistasController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContratistasController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Muestra la lista y contiene el Modal oculto
        public async Task<IActionResult> Index()
        {
            var lista = await _context.Contratistas
                .OrderBy(c => c.RazonSocial)
                .ToListAsync();
            return View(lista);
        }

        // POST: Sirve tanto para CREAR como para EDITAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(Contratista contratista)
        {
            if (ModelState.IsValid)
            {
                if (contratista.Id == 0)
                {
                    // Es NUEVO
                    _context.Add(contratista);
                    TempData["Success"] = "Contratista agregado correctamente.";
                }
                else
                {
                    // Es EDICIÓN
                    _context.Update(contratista);
                    TempData["Success"] = "Contratista actualizado correctamente.";
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si algo falla, recargamos la lista y mostramos error
            TempData["Error"] = "No se pudo guardar. Verifica los datos.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Eliminar
        // En ContratistasController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(int id)
        {
            // 1. VALIDACIÓN: Verificar si es ganador en alguna licitación
            bool esGanador = await _context.Licitaciones
                                           .AnyAsync(l => l.ContratistaGanadorId == id);

            if (esGanador)
            {
                // Si es ganador, NO borramos y mandamos el mensaje de error especial
                TempData["SweetAlertType"] = "error";
                TempData["SweetAlertTitle"] = "No se puede eliminar";
                TempData["SweetAlertMessage"] = "Este contratista ya ha sido asignado como GANADOR de un proyecto y no puede ser eliminado.";

                return RedirectToAction(nameof(Index));
            }

            // 2. Si pasa la validación, procedemos a borrar
            var contratista = await _context.Contratistas.FindAsync(id);
            if (contratista != null)
            {
                _context.Contratistas.Remove(contratista);
                await _context.SaveChangesAsync();

                // Mensaje de éxito
                TempData["SweetAlertType"] = "success";
                TempData["SweetAlertTitle"] = "Eliminado";
                TempData["SweetAlertMessage"] = "El contratista ha sido eliminado correctamente.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}