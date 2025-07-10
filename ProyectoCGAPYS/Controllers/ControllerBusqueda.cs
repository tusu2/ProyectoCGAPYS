using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos; // Tu DbContext
using ProyectoCGAPYS.Models; // Tus modelos de EF Core

namespace ProyectoCGAPYS.Controllers
{
    public class ProyectoController : Controller
    {
        private readonly ApplicationDbContext _context;

        // Inyectamos el DbContext para tener acceso a la base de datos
        public ProyectoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- ACCIÓN PRINCIPAL PARA MOSTRAR LA VISTA ---
        // Esto reemplaza la necesidad de tener un Busqueda.html estático
        public async Task<IActionResult> Index()
        {
            // Pasamos las dependencias a la vista para llenar el dropdown de filtros
            ViewBag.Dependencias = new SelectList(await _context.Dependencias.OrderBy(d => d.Nombre).ToListAsync(), "Id", "Nombre");
            return View();
        }

        // --- SECCIÓN DE API INTERNA ---
        // Estos métodos reemplazarán tu json-server. Serán llamados por tu JavaScript.

        [HttpGet]
        public async Task<JsonResult> GetProyectos()
        {
            // Usamos .Select() para crear un objeto anónimo y evitar problemas de referencias circulares
            // y para enviar solo los datos necesarios.
            var proyectos = await _context.Proyectos
                .Select(p => new {
                    p.Id,
                    p.NombreProyecto,
                    p.Folio,
                    p.Latitud,
                    p.Longitud,
                    Dependencia = p.Dependencia.Nombre, // Obtenemos el nombre de la dependencia relacionada
                    p.Descripcion,
                    p.FechaSolicitud,
                    p.FechaFinalizacionAprox,
                    p.Presupuesto,
                    TipoFondo = p.TipoFondo.Nombre, // Nombre del tipo de fondo
                    p.NombreResponsable,
                    p.Correo,

                    p.Celular,
                    p.NombreAnteproyecto,
                    TipoProyecto = p.TipoProyecto.Nombre, // Nombre del tipo de proyecto
                    Estatus = p.Estatus
                })
                .ToListAsync();

            return Json(proyectos);
        }

        [HttpGet]
        public async Task<JsonResult> GetCategorias()
        {
            var categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
            return Json(categorias);
        }

        [HttpGet]
        public async Task<JsonResult> GetConceptos()
        {
            var conceptos = await _context.Conceptos.ToListAsync();
            return Json(conceptos);
        }

        [HttpGet]
        public async Task<JsonResult> GetCostosPorProyecto(string id) // Recibimos el ID del proyecto
        {
            var costos = await _context.Proyectos_Costos
                                       .Where(pc => pc.IdProyectoFk == id)
                                       .ToListAsync();
            return Json(costos);
        }

        [HttpPost]
        public async Task<JsonResult> AnadirCosto([FromBody] Proyectos_Costos nuevoCosto)
        {
            if (ModelState.IsValid)
            {
                // Puedes generar un ID aquí si no lo haces en el frontend
                nuevoCosto.Id = Guid.NewGuid().ToString("N").Substring(0, 4);
                _context.Proyectos_Costos.Add(nuevoCosto);
                await _context.SaveChangesAsync();
                return Json(new { success = true, data = nuevoCosto });
            }
            return Json(new { success = false, errors = "Datos inválidos" });
        }

        [HttpDelete]
        public async Task<JsonResult> EliminarCosto(string id)
        {
            var costo = await _context.Proyectos_Costos.FindAsync(id);
            if (costo == null)
            {
                return Json(new { success = false, message = "Costo no encontrado" });
            }

            _context.Proyectos_Costos.Remove(costo);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Costo eliminado" });
        }
    }
}