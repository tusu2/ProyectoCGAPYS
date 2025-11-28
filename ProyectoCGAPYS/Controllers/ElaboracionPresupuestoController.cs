using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using System.Security.Claims;

namespace ProyectoCGAPYS.Controllers
{
    [Authorize]
    public class ElaboracionPresupuestoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ElaboracionPresupuestoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ElaboracionPresupuesto/Index
        // Muestra proyectos en Fase 3 (Elaboración de Presupuesto)
        public async Task<IActionResult> Index()
        {
            var proyectos = await _context.Proyectos
                .Include(p => p.Fase)
                .Include(p => p.Campus)
                .Include(p => p.UsuarioResponsable)
                // Fase 3 = Elaboración de Presupuesto
                .Where(p => p.IdFaseFk == 3)
                .ToListAsync();

            return View(proyectos);
        }

        // GET: ElaboracionPresupuesto/Detalles/{id}
        public async Task<IActionResult> Detalles(string id)
        {
            if (id == null) return NotFound();

            var proyecto = await _context.Proyectos
                .Include(p => p.Fase)
                .Include(p => p.Campus)
                .Include(p => p.Dependencia)
                .Include(p => p.TipoFondo)
                .Include(p => p.Documentos)
                .Include(p => p.UsuarioResponsable)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proyecto == null) return NotFound();

            return View(proyecto);
        }

        // POST: Guardar Presupuesto Base
        // Este método se llamará desde el botón pequeño junto al input en Detalles
        [HttpPost]
        public async Task<JsonResult> ActualizarPresupuesto(string proyectoId, decimal nuevoMonto)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return Json(new { success = false, message = "Proyecto no encontrado." });

            proyecto.Presupuesto = nuevoMonto;

            // Opcional: Registrar en historial si deseas rastrear cambios de monto
            // _context.HistorialFases.Add(...) 

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Presupuesto actualizado correctamente." });
        }

        // POST: Carga Masiva (Igual que anteproyecto)
        [HttpPost]
        public async Task<JsonResult> CargarDocumentosMasivos(string proyectoId, List<IFormFile> archivos, List<string> etiquetas)
        {
            if (archivos == null || archivos.Count == 0)
                return Json(new { success = false, message = "No se recibieron archivos." });

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proyectos");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            try
            {
                for (int i = 0; i < archivos.Count; i++)
                {
                    var archivo = archivos[i];
                    var etiqueta = etiquetas != null && etiquetas.Count > i ? etiquetas[i] : "Presupuesto";
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + archivo.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await archivo.CopyToAsync(stream);
                    }

                    var documento = new DocumentosProyecto
                    {
                        ProyectoId = proyectoId,
                        NombreArchivo = archivo.FileName,
                        RutaArchivo = "/uploads/proyectos/" + uniqueFileName,
                        Descripcion = etiqueta,
                        FechaSubida = DateTime.Now
                    };
                    _context.DocumentosProyectos.Add(documento);
                }
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"{archivos.Count} documentos subidos." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Avanzar Fase -> VA A LA FASE 4
        // POST: Avanzar Fase -> VA A LA FASE 4 (Con Validación de Presupuesto)
        [HttpPost]
        public async Task<JsonResult> Avanzar(string proyectoId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
                return Json(new { success = false, message = "Proyecto no encontrado." });

            // 1. Validación de Presupuesto (Seguridad)
            if (proyecto.Presupuesto <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = "No puedes avanzar sin asignar un Presupuesto Base válido. Por favor, ingrésalo y guárdalo."
                });
            }

            int faseActual = proyecto.IdFaseFk ?? 0;

            // 2. Lógica para crear el registro en la tabla Licitaciones
            // Esto asegura que al pasar a fase 4, ya exista el registro base para trabajar.
            var nuevaLicitacion = new Licitacion
            {
                ProyectoId = proyecto.Id,
                // Generamos un número único con fecha y hora para evitar duplicados
                NumeroLicitacion = $"LIC-{proyecto.Folio}-{DateTime.Now:yyyyMMddHHmmss}",
                Descripcion = proyecto.Descripcion,
                FechaInicio = DateTime.Now,
                FechaFinPropuestas = null,
                Estado = "Abierta",
                TipoProceso = "Adjudicacion Directa", // Valor por defecto inicial

                // Inicializamos los flags en falso
                TieneDiferimientoPago = false,
                TieneConvenio = false,
                TieneSuspension = false,

                // Fechas explícitas en null para evitar errores
                FechaInicioDiferimiento = null,
                FechaFinDiferimiento = null,
                FechaInicioConvenio = null,
                FechaFinConvenio = null,
                FechaInicioSuspension = null,
                FechaFinSuspension = null
            };

            // Agregamos la licitación al contexto
            _context.Licitaciones.Add(nuevaLicitacion);


            // 3. Actualizar la Fase del Proyecto
            proyecto.IdFaseFk = 4; // Fase "En Licitación" / "Validación Técnica"

            // 4. Guardar en el Historial
            _context.HistorialFases.Add(new HistorialFase
            {
                ProyectoId = proyectoId,
                FaseAnteriorId = faseActual,
                FaseNuevaId = 4,
                TipoCambio = "Presupuesto Aprobado",
                Comentario = $"Presupuesto validado por {proyecto.Presupuesto:C2}. Se generó la Licitación {nuevaLicitacion.NumeroLicitacion}.",
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                FechaCambio = DateTime.Now
            });

            // 5. Guardar todos los cambios (Proyecto + Licitación + Historial)
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Proyecto avanzado y licitación generada exitosamente." });
        }

        // POST: Rechazar -> REGRESA A FASE 2 (Sin borrar prioridad)
        [HttpPost]
        public async Task<JsonResult> Rechazar(string proyectoId, string comentario)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return Json(new { success = false, message = "Proyecto no encontrado." });

            int faseActual = proyecto.IdFaseFk ?? 0;
            int nuevaFase = 2; // <--- REGRESA A ANTEPROYECTO

            // NOTA: Ya NO borramos la prioridad aquí.
            // proyecto.Prioridad = null; <--- COMENTADO

            proyecto.IdFaseFk = nuevaFase;

            _context.HistorialFases.Add(new HistorialFase
            {
                ProyectoId = proyectoId,
                FaseAnteriorId = faseActual,
                FaseNuevaId = nuevaFase,
                TipoCambio = "Rechazado (Regresa a Anteproyecto)",
                Comentario = comentario,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                FechaCambio = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Proyecto regresado a Anteproyecto." });
        }
    }
}