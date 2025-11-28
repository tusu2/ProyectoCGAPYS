using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using System.Security.Claims;

namespace ProyectoCGAPYS.Controllers
{
    [Authorize]
    public class AnteproyectoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AnteproyectoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Anteproyecto/Index
        // Muestra la lista de proyectos que están en la fase "Anteproyecto"
        public async Task<IActionResult> Index()
        {
            var proyectosAnteproyecto = await _context.Proyectos
                .Include(p => p.Fase)
                .Include(p => p.Campus)
                .Include(p => p.UsuarioResponsable)
                // Asumimos que la fase se llama "Anteproyecto" o es la ID 1. 
                // Ajusta "Anteproyecto" si en tu BD tiene otro nombre exacto (ej. "Inicio").
                .Where(p => p.IdFaseFk == 2)
                .ToListAsync();

            return View(proyectosAnteproyecto);
        }

        // GET: Anteproyecto/Detalles/{id}
        // Esta es la vista modificada: Sin presupuesto, con carga masiva, etc.
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

        // POST: Carga Masiva de Documentos (Se mantiene igual)
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
                    var etiqueta = etiquetas != null && etiquetas.Count > i ? etiquetas[i] : "Sin etiqueta";
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
                return Json(new { success = true, message = $"{archivos.Count} documentos subidos correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Avanzar Fase
        [HttpPost]
        public async Task<JsonResult> Avanzar(string proyectoId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return Json(new { success = false, message = "Proyecto no encontrado." });

            int faseActual = proyecto.IdFaseFk ?? 0;
            proyecto.IdFaseFk = faseActual + 1; // Avanza a la siguiente (ej. de 1 a 2)

            _context.HistorialFases.Add(new HistorialFase
            {
                ProyectoId = proyectoId,
                FaseAnteriorId = faseActual,
                FaseNuevaId = proyecto.IdFaseFk,
                TipoCambio = "Aprobado (Anteproyecto)",
                Comentario = "Aprobado desde módulo de Anteproyecto",
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                FechaCambio = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Proyecto avanzado exitosamente." });
        }

        // POST: Rechazar y Quitar Prioridad
        [HttpPost]
        public async Task<JsonResult> Rechazar(string proyectoId, string comentario)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return Json(new { success = false, message = "Proyecto no encontrado." });

            int faseActual = proyecto.IdFaseFk ?? 0;
            // Si está en fase 1, se queda en 1 o va a 0 según tu lógica. Aquí asumimos que retrocede si es > 1.
            int nuevaFase = faseActual > 1 ? faseActual - 1 : 1;

            // --- LÓGICA CLAVE: QUITAR PRIORIDAD ---
            proyecto.Prioridad = null;
            // -------------------------------------

            proyecto.IdFaseFk = nuevaFase;

            _context.HistorialFases.Add(new HistorialFase
            {
                ProyectoId = proyectoId,
                FaseAnteriorId = faseActual,
                FaseNuevaId = nuevaFase,
                TipoCambio = "Rechazado (Anteproyecto)",
                Comentario = comentario,
                UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                FechaCambio = DateTime.Now
            });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Proyecto rechazado y prioridad eliminada." });
        }
    }
}