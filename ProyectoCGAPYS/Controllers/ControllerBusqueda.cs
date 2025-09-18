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
            var proyectos = await _context.Proyectos
                .Include(p => p.Dependencia)
                .Include(p => p.TipoFondo)      // Asegúrate de incluir todas las relaciones
                .Include(p => p.TipoProyecto)   // que vayas a usar.
                .Select(p => new {
                    p.Id,
                    p.NombreProyecto,
                    p.Folio,
                    p.Latitud,
                    p.Longitud,

                    // --- AQUÍ ESTÁ LA CORRECCIÓN ---
                    // Si la dependencia no es null, usa su nombre. Si es null, usa un texto por defecto.
                    Dependencia = p.Dependencia != null ? p.Dependencia.Nombre : "No asignada",

                    p.Descripcion,
                    p.FechaSolicitud,
                    p.FechaFinalizacionAprox,
                    p.Presupuesto,

                    // Hacemos lo mismo para las otras relaciones
                    TipoFondo = p.TipoFondo != null ? p.TipoFondo.Nombre : "No asignado",

                    p.NombreResponsable,
                    p.Correo,
                    p.Celular,
                    p.NombreAnteproyecto,

                    TipoProyecto = p.TipoProyecto != null ? p.TipoProyecto.Nombre : "No asignado",

                    Estatus = p.Estatus,
                    p.Prioridad
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
        [HttpPost]
        public async Task<IActionResult> ActualizarPrioridad(string id, string prioridad)
        {
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null)
            {
                return NotFound(); // No se encontró el proyecto
            }

            // Validamos que la prioridad sea uno de los valores permitidos
            if (prioridad == "verde" || prioridad == "amarillo" || prioridad == "rojo")
            {
                proyecto.Prioridad = prioridad;
                await _context.SaveChangesAsync();
                return Ok(); // Todo salió bien
            }

            return BadRequest("Prioridad no válida"); // Error si el valor no es correcto
        }

        // En ProyectoController.cs

        public async Task<IActionResult> AsignarPrioridades()
        {
            var proyectosSinPrioridad = await _context.Proyectos
                                   .Where(p => string.IsNullOrEmpty(p.Prioridad))
                                   .Include(p => p.Dependencia)
                                   .ToListAsync();

            return PartialView("_AsignarPrioridadesPartial", proyectosSinPrioridad);
        }

        [HttpGet]
        public async Task<JsonResult> GetProyectoDetalles(string id)
        {
            var proyecto = await _context.Proyectos
                .Include(p => p.Campus) // Incluimos todas las relaciones para tener los nombres
                .Include(p => p.Fase)
                .FirstOrDefaultAsync(p => p.Id == id); // Buscamos el proyecto específico por su ID

            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado" });
            }

            // Creamos un objeto con los datos que queremos mostrar
            var detalles = new
            {
                success = true,
                nombreResponsable = proyecto.NombreResponsable,
                correo = proyecto.Correo,
                celular = proyecto.Celular,
                fechaSolicitud = proyecto.FechaSolicitud?.ToString("dd/MM/yyyy"),
                fechaFinalizacion = proyecto.FechaFinalizacionAprox?.ToString("dd/MM/yyyy"),
                presupuesto = proyecto.Presupuesto.ToString("C"), // Formato de moneda
                campus = proyecto.Campus?.Nombre,
                fase = proyecto.Fase?.Nombre ?? "No definida",
                descripcion = proyecto.Descripcion
            };

            return Json(detalles);
        }

        // En ProyectoController.cs

        public async Task<IActionResult> GetIndexContent()
        {
            // Preparamos el ViewBag para el dropdown de filtros, ya que la vista parcial lo necesita
            ViewBag.Dependencias = new SelectList(await _context.Dependencias.OrderBy(d => d.Nombre).ToListAsync(), "Id", "Nombre");

            return PartialView("_IndexContentPartial");
        }
    }
}