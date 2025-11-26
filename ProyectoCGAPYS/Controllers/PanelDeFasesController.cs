using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using System.Security.Claims;

namespace ProyectoCGAPYS.Controllers
{
    [Authorize]
    public class PanelDeFasesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public PanelDeFasesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var proyectosActivos = await _context.Proyectos
                .Include(p => p.Fase)
                .Where(p => p.Fase != null && p.Fase.Nombre != "Cancelado")
                .ToListAsync();

            var todasLasFases = await _context.Fases.OrderBy(f => f.Orden).ToListAsync();

            ViewBag.Fases = todasLasFases;
            return View("Index", proyectosActivos);
        }

        [HttpPost]
        public async Task<JsonResult> CambiarFase(string proyectoId, int nuevaFaseId)
        {
            var proyecto = await _context.Proyectos.Include(p => p.Fase).FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado." });
            }

            // --- NUEVO: BLOQUEO DE FASES ---
            string[] fasesBloqueadas = { "En Licitación", "En Ejecución", "Finalizado", "Cancelado" };
            if (proyecto.Fase != null && fasesBloqueadas.Contains(proyecto.Fase.Nombre))
            {
                return Json(new { success = false, message = $"No se puede mover un proyecto que está en fase: {proyecto.Fase.Nombre}" });
            }
            // -------------------------------

            proyecto.IdFaseFk = nuevaFaseId;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Fase actualizada correctamente." });
        }

        public async Task<IActionResult> Detalles(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var proyecto = await _context.Proyectos
                .Include(p => p.Fase)
                .Include(p => p.Campus)
                .Include(p => p.Dependencia)
                .Include(p => p.TipoFondo)
                .Include(p => p.TipoProyecto)
                .Include(p => p.Documentos)
                .Include(p => p.UsuarioResponsable)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proyecto == null)
            {
                return NotFound();
            }

            var historialParaLaVista = await _context.HistorialFases
                .Include(h => h.Usuario)
                .Where(h => h.ProyectoId == id)
                .OrderByDescending(h => h.FechaCambio)
                .Select(h => new HistorialViewModel
                {
                    FechaCambio = h.FechaCambio,
                    TipoCambio = h.TipoCambio,
                    Comentario = h.Comentario,
                    NombreUsuario = h.Usuario.UserName ?? "Sistema"
                })
                .ToListAsync();

            ViewBag.Historial = historialParaLaVista;

            return View(proyecto);
        }

        [HttpPost]
        public async Task<JsonResult> RechazarFase(string proyectoId, string comentario)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado." });
            }
            string[] fasesBloqueadas = { "En Licitación", "En Ejecución", "Finalizado", "Cancelado" };

            if (proyecto.Fase != null && fasesBloqueadas.Contains(proyecto.Fase.Nombre))
            {
                return Json(new { success = false, message = "No se pueden rechazar proyectos en esta fase." });
            }
            if (string.IsNullOrWhiteSpace(comentario))
            {
                return Json(new { success = false, message = "El comentario de rechazo no puede estar vacío." });
            }

            int faseActualId = proyecto.IdFaseFk ?? 0;
            int nuevaFaseId = faseActualId > 1 ? faseActualId - 1 : 1;
            proyecto.IdFaseFk = nuevaFaseId;
            var registroHistorial = new HistorialFase
            {
                ProyectoId = proyectoId,
                FaseAnteriorId = proyecto.IdFaseFk,
                FaseNuevaId = proyecto.IdFaseFk,
                Comentario = comentario,
                TipoCambio = "Rechazado"
            };

            _context.HistorialFases.Add(registroHistorial);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "El rechazo ha sido registrado en el historial." });
        }

        [HttpPost]
        public async Task<JsonResult> AvanzarFaseProyecto(string proyectoId)
        {
            var proyecto = await _context.Proyectos
                                    .Include(p => p.TipoProyecto)
                                    .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado." });
            }

            int faseActualId = proyecto.IdFaseFk ?? 0;
            int nuevaFaseId = faseActualId;

            switch (faseActualId)
            {
                case 1:
                    if (proyecto.TipoProyecto.Nombre.Contains("Mantenimiento"))
                    {
                        nuevaFaseId = 3;
                    }
                    else
                    {
                        nuevaFaseId = 2;
                    }
                    break;
                case 2:
                    nuevaFaseId = 3;
                    break;
                case 3:
                    nuevaFaseId = 4; // El siguiente paso es "En Licitación"
                    break;
            }

            if (nuevaFaseId == faseActualId)
            {
                return Json(new { success = false, message = "El proyecto ya se encuentra en la última fase del flujo definido." });
            }

            // ******************** INICIO DE LA MODIFICACIÓN ********************
            // Si la nueva fase es "En Licitación" (ID 4), creamos el registro.

            if (nuevaFaseId == 4)
            {
                var nuevaLicitacion = new Licitacion
                {
                    ProyectoId = proyecto.Id,
                    // Generamos un número de licitación único basado en la fecha y hora.
                    NumeroLicitacion = $"LIC-{proyecto.Folio}-{DateTime.Now:yyyyMMdd}",
                    Descripcion = proyecto.Descripcion, // Usamos la descripción del proyecto.
                    FechaInicio = DateTime.Now, // La fecha y hora actual.
                    FechaFinPropuestas = null, // Como se solicitó, se deja en null.
                    Estado = "Abierta",
                     TipoProceso = "Adjudicacion Directa",
                    TieneDiferimientoPago = false,
                    TieneConvenio = false,
                    TieneSuspension = false,

                    // Aseguramos que las fechas sean null explícitamente
                    FechaInicioDiferimiento = null,
                    FechaFinDiferimiento = null,
                    FechaInicioConvenio = null,
                    FechaFinConvenio = null,
                    FechaInicioSuspension = null,
                    FechaFinSuspension = null// Estado por defecto.
                };
                _context.Licitaciones.Add(nuevaLicitacion);
            }
            // ********************* FIN DE LA MODIFICACIÓN **********************

            proyecto.IdFaseFk = nuevaFaseId;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var registroHistorial = new HistorialFase
            {
                ProyectoId = proyectoId,
                FaseAnteriorId = faseActualId,
                FaseNuevaId = nuevaFaseId,
                TipoCambio = "Aprobado",
                Comentario = "Avance de fase automático.",
                UsuarioId = userId
            };
            _context.HistorialFases.Add(registroHistorial);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "El proyecto ha avanzado.", nuevaFaseId = nuevaFaseId });
        }

        public class CambioFaseRequest
        {
            public string ProyectoId { get; set; }
            public int NuevaFaseId { get; set; }
            public string Tipo { get; set; }
            public string Comentario { get; set; }
        }
        [HttpPost]
        public async Task<JsonResult> GuardarCambiosDeFase([FromBody] List<CambioFaseRequest> cambios)
        {
            if (cambios == null || !cambios.Any())
            {
                return Json(new { success = false, message = "No se recibieron cambios." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            foreach (var cambio in cambios)
            {
                var proyecto = await _context.Proyectos.FindAsync(cambio.ProyectoId);
                if (proyecto != null)
                {
                    var faseActualId = proyecto.IdFaseFk ?? 0;

                    if (cambio.NuevaFaseId == 4 && faseActualId != 4)
                    {
                        var nuevaLicitacion = new Licitacion
                        {
                            ProyectoId = proyecto.Id,
                            // CORRECCIÓN AQUÍ: Agregamos HHmmss para evitar duplicados
                            NumeroLicitacion = $"LIC-{proyecto.Folio}-{DateTime.Now:yyyyMMddHHmmss}",
                            Descripcion = proyecto.Descripcion,
                            FechaInicio = DateTime.Now,
                            FechaFinPropuestas = null,
                            Estado = "Abierta",
                            TipoProceso = "Adjudicacion directa",

                            // Los booleanos que ya tenías bien
                            TieneDiferimientoPago = false,
                            TieneConvenio = false,
                            TieneSuspension = false,
                            FechaInicioDiferimiento = null, // Explícitos para seguridad extra
                            FechaFinDiferimiento = null,
                            FechaInicioConvenio = null,
                            FechaFinConvenio = null,
                            FechaInicioSuspension = null,
                            FechaFinSuspension = null
                        };
                        _context.Licitaciones.Add(nuevaLicitacion);
                    }

                    proyecto.IdFaseFk = cambio.NuevaFaseId;

                    var registroHistorial = new HistorialFase
                    {
                        ProyectoId = cambio.ProyectoId,
                        FaseAnteriorId = faseActualId,
                        FaseNuevaId = cambio.NuevaFaseId,
                        TipoCambio = cambio.Tipo == "Avance" ? "Aprobado (Arrastre)" : "Devuelto (Arrastre)",
                        Comentario = cambio.Comentario,
                        UsuarioId = userId
                    };
                    _context.HistorialFases.Add(registroHistorial);
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "¡Todos los cambios han sido guardados!" });
        }

        // ... (resto de los métodos sin cambios)

        [HttpPost]
        public async Task<IActionResult> SubirDocumento(string proyectoId, IFormFile archivo, string descripcion)
        {
            if (archivo == null || archivo.Length == 0)
            {
                return BadRequest("No se ha seleccionado ningún archivo.");
            }

            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return NotFound();

            // Creamos una ruta segura para guardar el archivo
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "proyectos");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

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
                RutaArchivo = "/uploads/proyectos/" + uniqueFileName, // Ruta web
                Descripcion = descripcion
            };

            _context.DocumentosProyectos.Add(documento);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detalles", new { id = proyectoId });
        }

        [HttpGet]
        public async Task<IActionResult> DescargarDocumento(int documentoId)
        {
            var documento = await _context.DocumentosProyectos.FindAsync(documentoId);
            if (documento == null) return NotFound();

            var memory = new MemoryStream();
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", documento.RutaArchivo.TrimStart('/'));
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/octet-stream", documento.NombreArchivo);
        }
    }
}