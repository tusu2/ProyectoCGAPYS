using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models;
using System.Security.Claims;
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
            var user = await _userManager.GetUserAsync(User); // Obtenemos el usuario actual
            var proyectosActivos = await _context.Proyectos
            .Include(p => p.Fase)
            .Where(p => p.Fase != null && p.Fase.Nombre != "Finalizado" && p.Fase.Nombre != "Cancelado")
            .ToListAsync();

            var todasLasFases = await _context.Fases.OrderBy(f => f.Orden).ToListAsync();

            ViewBag.Fases = todasLasFases;
            return View("Index", proyectosActivos);
        }

        // GET: PanelDeFasesController/Details/5


        [HttpPost]
        public async Task<JsonResult> CambiarFase(string proyectoId, int nuevaFaseId)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado." });
            }

            proyecto.IdFaseFk = nuevaFaseId; // ¡Aquí ocurre el cambio de fase!
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Fase actualizada correctamente." });
        }



        public async Task<IActionResult> Detalles(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Esta parte para obtener el proyecto principal está correcta.
            var proyecto = await _context.Proyectos
                .Include(p => p.Fase)
                .Include(p => p.Campus)
                .Include(p => p.Dependencia)
                .Include(p => p.TipoFondo)
                .Include(p => p.TipoProyecto)
                .Include(p => p.Documentos)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (proyecto == null)
            {
                return NotFound();
            }

            // --- SECCIÓN ACTUALIZADA ---
            // Aquí transformamos los datos del historial al ViewModel que la vista necesita.
            var historialParaLaVista = await _context.HistorialFases
                .Include(h => h.Usuario) // Importante: Carga los datos del usuario relacionado
                .Where(h => h.ProyectoId == id)
                .OrderByDescending(h => h.FechaCambio)
                .Select(h => new HistorialViewModel // "Traducimos" cada registro
                {
                    FechaCambio = h.FechaCambio,
                    TipoCambio = h.TipoCambio,
                    Comentario = h.Comentario,
                    // Acceso seguro al nombre de usuario. Si no hay usuario, muestra "Sistema".
                    NombreUsuario = h.Usuario.UserName ?? "Sistema"
                })
                .ToListAsync();

            ViewBag.Historial = historialParaLaVista;

            return View(proyecto);
        }

        // En ProyectoController.cs

        [HttpPost]
        public async Task<JsonResult> RechazarFase(string proyectoId, string comentario)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado." });
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
                FaseAnteriorId = proyecto.IdFaseFk, // Guardamos la fase en la que estaba
                FaseNuevaId = proyecto.IdFaseFk,   // No cambia de fase, solo se anota el rechazo
                Comentario = comentario,
                TipoCambio = "Rechazado"
                // La fecha se genera automáticamente por la base de datos
            };

            _context.HistorialFases.Add(registroHistorial);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "El rechazo ha sido registrado en el historial." });
        }

        // En ProyectoController.cs

        [HttpPost]
        public async Task<JsonResult> AvanzarFaseProyecto(string proyectoId)
        {
            var proyecto = await _context.Proyectos
                                    .Include(p => p.TipoProyecto) // Incluimos el Tipo de Proyecto para la decisión
                                    .FirstOrDefaultAsync(p => p.Id == proyectoId);

            if (proyecto == null)
            {
                return Json(new { success = false, message = "Proyecto no encontrado." });
            }

            int faseActualId = proyecto.IdFaseFk ?? 0;
            int nuevaFaseId = faseActualId;

            // --- AQUÍ VIVEN LAS REGLAS DE NEGOCIO DEL DIAGRAMA DE FLUJO ---
            switch (faseActualId)
            {
                case 1: // Si está en "Recepción / Análisis"
                        // Esta es la bifurcación clave
                    if (proyecto.TipoProyecto.Nombre.Contains("Mantenimiento"))
                    {
                        nuevaFaseId = 3; // Va directo a "En Elaboración de Presupuesto"
                    }
                    else // Si es Obra Nueva o cualquier otro tipo
                    {
                        nuevaFaseId = 2; // Va a "En Elaboración de Anteproyecto"
                    }
                    break;

                case 2: // Si está en "En Elaboración de Anteproyecto"
                    nuevaFaseId = 3; // El siguiente paso es "En Elaboración de Presupuesto"
                    break;

                case 3: // Si está en "En Elaboración de Presupuesto"
                    nuevaFaseId = 4; // El siguiente paso es "En Licitación"
                    break;

                    // Aquí podrías añadir más reglas para las fases futuras (de Licitación a Ejecución, etc.)
            }

            // Si no hubo un cambio de fase válido, devolvemos un error.
            if (nuevaFaseId == faseActualId)
            {
                return Json(new { success = false, message = "El proyecto ya se encuentra en la última fase del flujo definido." });
            }

            // Actualizamos el proyecto y guardamos el historial
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
            foreach (var cambio in cambios)
            {
                var proyecto = await _context.Proyectos.FindAsync(cambio.ProyectoId);
                if (proyecto != null)
                {
                    var faseActualId = proyecto.IdFaseFk;
                    proyecto.IdFaseFk = cambio.NuevaFaseId;

                    var registroHistorial = new HistorialFase
                    {
                        ProyectoId = cambio.ProyectoId,
                        FaseAnteriorId = faseActualId,
                        FaseNuevaId = cambio.NuevaFaseId,
                        TipoCambio = cambio.Tipo == "Avance" ? "Aprobado (Arrastre)" : "Devuelto (Arrastre)",
                        // ¡USAREMOS EL COMENTARIO QUE NOS LLEGA!
                        Comentario = cambio.Comentario,


                    };
                    _context.HistorialFases.Add(registroHistorial);
                }
            }


            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "¡Todos los cambios han sido guardados!" });
        }
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
