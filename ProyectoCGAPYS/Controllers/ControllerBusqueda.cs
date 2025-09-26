using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos; // Tu DbContext
using ProyectoCGAPYS.Models; // Tus modelos de EF Core

namespace ProyectoCGAPYS.Controllers
{
    [Authorize(Roles = "Jefa")]
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
            ViewBag.CampusList = new SelectList(await _context.Campus.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");
            return View();
        }

        // --- SECCIÓN DE API INTERNA ---
        // Estos métodos reemplazarán tu json-server. Serán llamados por tu JavaScript.

        [HttpGet]
        public async Task<JsonResult> GetProyectos()
        {
            var proyectos = await _context.Proyectos
                .Include(p => p.Dependencia) // Puedes dejar estas o quitarlas si ya no las usas
                .Include(p => p.TipoFondo)
                .Include(p => p.TipoProyecto)
                .Include(p => p.Campus) // <-- AÑADE ESTA LÍNEA
                .Select(p => new {
                    p.Id,
                    p.NombreProyecto,
                    p.Folio,
                    p.Latitud,
                    p.Longitud,

                    // --- CÓDIGO ACTUALIZADO ---
                    IdCampusFk = p.IdCampusFk, // <-- AÑADE ESTA LÍNEA
                    Campus = p.Campus != null ? p.Campus.Nombre : "No asignado", // <-- AÑADE ESTA LÍNEA

                    // Puedes borrar las siguientes dos líneas si ya no usas Dependencia
                    IdDependenciaFk = p.IdDependenciaFk,
                    Dependencia = p.Dependencia != null ? p.Dependencia.Nombre : "No asignada",

                    p.Descripcion,
                    p.FechaSolicitud,
                    // ... el resto de tus campos ...
                    p.Presupuesto,
                    TipoFondo = p.TipoFondo != null ? p.TipoFondo.Nombre : "No asignado",
                    p.NombreResponsable,
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

        public async Task<IActionResult> AsignarPrioridades(
       string? nombre,
       int? campusId,
       DateTime? fechaInicio,
       DateTime? fechaFin,
       decimal? presupuestoMin,
       decimal? presupuestoMax)
        {
   
            IQueryable<Proyectos> query = _context.Proyectos
                                                .Where(p => string.IsNullOrEmpty(p.Prioridad))
                                                .Include(p => p.Dependencia);

          
            if (!string.IsNullOrEmpty(nombre))
            {
                query = query.Where(p => EF.Functions.Collate(p.NombreProyecto, "SQL_Latin1_General_CP1_CI_AI").Contains(nombre));
            }
            if (campusId.HasValue)
            {
                query = query.Where(p => p.IdCampusFk == campusId.Value);
            }
            if (fechaInicio.HasValue)
            {
                query = query.Where(p => p.FechaSolicitud >= fechaInicio.Value);
            }
            if (fechaFin.HasValue)
            {
              
                query = query.Where(p => p.FechaSolicitud < fechaFin.Value.AddDays(1));
            }
            if (presupuestoMin.HasValue)
            {
                query = query.Where(p => p.Presupuesto >= presupuestoMin.Value);
            }
            if (presupuestoMax.HasValue)
            {
                query = query.Where(p => p.Presupuesto <= presupuestoMax.Value);
            }

          
            ViewBag.CampusList = new SelectList(await _context.Campus.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre", campusId);

            var proyectosFiltrados = await query.ToListAsync();

            return PartialView("_AsignarPrioridadesPartial", proyectosFiltrados);
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
            ViewBag.CampusList = new SelectList(await _context.Campus.OrderBy(c => c.Nombre).ToListAsync(), "Id", "Nombre");

            return PartialView("_IndexContentPartial");
        }

      
    }
}