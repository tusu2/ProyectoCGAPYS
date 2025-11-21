using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // Necesario para IWebHostEnvironment
using System.IO;
using System;
using ProyectoCGAPYS.Models; // Asegúrate de tener este using para las entidades

namespace ProyectoCGAPYS.Controllers
{
    [Authorize]
    public class EstimacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // Nuevo servicio para archivos

        // Inyectamos los servicios (Agregamos IWebHostEnvironment)
        public EstimacionesController(ApplicationDbContext context,
                                      UserManager<IdentityUser> userManager,
                                      IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Estimaciones/Detalles/5
        // Acción para ver el detalle de una estimación específica
        public async Task<IActionResult> Detalles(int id)
        {
            var estimacion = await _context.Estimaciones
                .Include(e => e.Proyecto)
                .Include(e => e.Documentos)
                .Include(e => e.Historial)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (estimacion == null)
            {
                return NotFound();
            }

            // Buscar datos del contratista para mostrarlos en la vista
            // Asumimos que la licitación ganadora conecta el proyecto con el contratista
            var licitacionGanadora = await _context.Licitaciones
                .Include(l => l.ContratistaGanador)
                .FirstOrDefaultAsync(l => l.ProyectoId == estimacion.IdProyectoFk && l.ContratistaGanadorId != null);

            if (licitacionGanadora != null)
            {
                ViewBag.Contratista = licitacionGanadora.ContratistaGanador;
            }

            return View(estimacion);
        }

        // En ProyectosController.cs

        public async Task<IActionResult> DashboardPorProyecto(string id) // Recibe el ID del proyecto
        {
            if (id == null) return NotFound();

            // 1. Obtener el proyecto para sacar el nombre y folio
            var proyecto = await _context.Proyectos.FindAsync(id);
            if (proyecto == null) return NotFound();

            // 2. Obtener las estimaciones de este proyecto
            var estimaciones = await _context.Estimaciones
                .Include(e => e.Historial)
                .Where(e => e.IdProyectoFk == id)
                .ToListAsync();

            // 3. Agruparlas por estado (para el Kanban)
            var diccionarioEstimaciones = estimaciones
                .GroupBy(e => e.Estado)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 4. Pasar datos a la vista (IMPORTANTE: ViewBag.ProyectoId)
            ViewBag.ProyectoNombre = proyecto.NombreProyecto;
            ViewBag.ProyectoFolio = proyecto.Folio;
            ViewBag.ProyectoId = proyecto.Id; // <--- ESTO ES VITAL PARA EL FORMULARIO DE CREAR

            return View(diccionarioEstimaciones);
        }

        // POST: /Estimaciones/CrearEstimacion
        // Esta es la lógica que antes hacía el Contratista, ahora adaptada para el SUPERVISOR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEstimacion([Bind(Prefix = "NuevaEstimacion")] EstimacionCrearViewModel viewModel)
        {
            // Validar que venga el ID del proyecto
            if (string.IsNullOrEmpty(viewModel.IdProyectoFk))
            {
                ModelState.AddModelError("NuevaEstimacion.IdProyectoFk", "El ID del proyecto es requerido.");
            }

            // Limpiamos validaciones del DropDown que ya no usamos aquí si viene nulo
            ModelState.Remove("NuevaEstimacion.ProyectosAsignados");
            ModelState.Remove("ProyectosAsignados");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Datos inválidos: " + string.Join("; ", errors);
          
                return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { id = viewModel.IdProyectoFk });
          
            }

            var usuarioActual = await _userManager.GetUserAsync(User);

            // 1. Crear la entidad principal (la Estimación)
            var estimacion = new Estimaciones
            {
                IdProyectoFk = viewModel.IdProyectoFk,
                Monto = viewModel.Monto,
                FechaEstimacion = viewModel.FechaEstimacion,
                Descripcion = viewModel.Descripcion,

                // CAMBIO IMPORTANTE: El estado inicial es directo a Control de Obra
                Estado = "En Revisión Control Obra"
            };

            _context.Estimaciones.Add(estimacion);
            await _context.SaveChangesAsync(); // Guardamos para generar el ID

            try
            {
                // 2. Guardar archivos (Usando el Helper privado abajo)
                await GuardarArchivoEstimacion(estimacion.Id, viewModel.ArchivoNumerosGeneradores, "NumerosGeneradores", usuarioActual.Id);
                await GuardarArchivoEstimacion(estimacion.Id, viewModel.ArchivoReporteFotografico, "ReporteFotografico", usuarioActual.Id);
                await GuardarArchivoEstimacion(estimacion.Id, viewModel.ArchivoBitacora, "Bitacora", usuarioActual.Id);

                // 3. Crear el primer registro en el Historial
                var historial = new EstimacionHistorial
                {
                    EstimacionId = estimacion.Id,
                    EstadoAnterior = "N/A", // No había estado anterior
                    EstadoNuevo = "En Revisión Control Obra",
                    UsuarioId = usuarioActual.Id,
                    Comentario = "Estimación generada por Supervisor. Enviada a Control de Obra."
                };
                _context.EstimacionHistorial.Add(historial);

                // 4. Guardar cambios finales
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Estimación creada y enviada a Control de Obra exitosamente.";
            }
            catch (Exception ex)
            {
                // Si falla algo con los archivos, borramos la estimación para no dejar basura
                _context.Estimaciones.Remove(estimacion);
                await _context.SaveChangesAsync();
                TempData["Error"] = "Error al guardar los archivos: " + ex.Message;
            }

            // Redirigir a la vista donde se ven las estimaciones del proyecto
            // Aquí asumo que usas la vista que modificamos anteriormente llamada DetallesLicitacion pero usada por admin
            // O redirigir a donde sea que el supervisor vea el tablero.
            return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { id = viewModel.IdProyectoFk });
        }


        // --- LOGICA DE APROBACIÓN / RECHAZO (CONTROL DE OBRA) ---

        [HttpPost]
        public async Task<IActionResult> AprobarControlObra(int estimacionId)
        {
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();

            var usuarioActual = await _userManager.GetUserAsync(User);

            // Cambiar estado
            string estadoAnterior = estimacion.Estado;
            estimacion.Estado = "Aprobado (Pendiente Factura)";

            // Historial
            var historial = new EstimacionHistorial
            {
                EstimacionId = estimacion.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estimacion.Estado,
                UsuarioId = usuarioActual.Id,
                Comentario = "Documentación validada por Control de Obra. Se solicita factura."
            };
            _context.EstimacionHistorial.Add(historial);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estimación aprobada. Esperando factura.";
            return RedirectToAction("Detalles", new { id = estimacionId });
        }

        [HttpPost]
        public async Task<IActionResult> RechazarControlObra(int estimacionId, string comentario)
        {
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();

            var usuarioActual = await _userManager.GetUserAsync(User);

            string estadoAnterior = estimacion.Estado;
            // Regresa al Supervisor para que corrija (En tu flujo, como el supervisor la crea, se la devuelves a él)
            // O podrías ponerle un estado "Devuelto a Supervisor"
            // Por simplicidad usaremos "En Creación" o un estado que indique corrección
            estimacion.Estado = "En Creación"; // O "Rechazado por Control Obra"

            var historial = new EstimacionHistorial
            {
                EstimacionId = estimacion.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estimacion.Estado,
                UsuarioId = usuarioActual.Id,
                Comentario = "RECHAZADO Control Obra: " + comentario
            };
            _context.EstimacionHistorial.Add(historial);
            await _context.SaveChangesAsync();

            TempData["Error"] = "Estimación rechazada y devuelta al Supervisor.";
            return RedirectToAction("Detalles", new { id = estimacionId });
        }

        // ... Aquí puedes agregar los métodos de Tesorería (SubirDocumentoInterno, EnviarATesoreria, MarcarComoPagada) ...
        // Si ya los tenías, asegúrate de mantenerlos.


        // --- HELPER PRIVADO PARA GUARDAR ARCHIVOS ---
        private async Task GuardarArchivoEstimacion(int estimacionId, IFormFile archivo, string tipoDocumento, string usuarioId)
        {
            if (archivo == null || archivo.Length == 0)
            {
                // Dependiendo de tu regla de negocio, puedes lanzar error o simplemente ignorar si es opcional
                // throw new Exception($"El archivo para '{tipoDocumento}' es obligatorio.");
                return;
            }

            // 1. Definir ruta
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "estimaciones");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(archivo.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 2. Guardar en disco
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(fileStream);
            }

            // 3. Guardar en BD
            var documento = new EstimacionDocumentos
            {
                EstimacionId = estimacionId,
                TipoDocumento = tipoDocumento,
                NombreArchivo = Path.GetFileName(archivo.FileName),
                RutaArchivo = "/uploads/estimaciones/" + uniqueFileName,
                UsuarioId = usuarioId,
                FechaSubida = DateTime.Now
            };

            _context.EstimacionDocumentos.Add(documento);
            // No hacemos SaveChanges aquí, se hace en el método principal
        }
    }
}