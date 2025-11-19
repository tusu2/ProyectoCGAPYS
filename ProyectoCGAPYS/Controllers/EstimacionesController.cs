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
            ViewBag.EstaBloqueado = proyecto.EstaBloqueado;
            ViewBag.SLAStatus = "N/A"; // (OK, Advertencia, Vencido)
            ViewBag.SLADias = 0;

            var licitacion = await _context.Licitaciones
    .Where(l => l.ProyectoId == id && l.Estado == "Adjudicada")
    .OrderByDescending(l => l.FechaFallo)
    .FirstOrDefaultAsync();

            bool tienePrimeraEstimacion = await _context.Estimaciones.AnyAsync(e => e.IdProyectoFk == id);
            if (licitacion?.FechaInicioEjecucion != null && !tienePrimeraEstimacion)
            {
                var diasTranscurridos = (DateTime.Now - licitacion.FechaInicioEjecucion.Value).TotalDays;
                ViewBag.SLADias = (int)Math.Floor(30 - diasTranscurridos); // Días restantes

                if (diasTranscurridos <= 20) ViewBag.SLAStatus = "OK";
                else if (diasTranscurridos <= 30) ViewBag.SLAStatus = "Advertencia";
                else if (diasTranscurridos <= 40) ViewBag.SLAStatus = "Vencido";
                else ViewBag.SLAStatus = "Bloqueado"; // Ya pasó los 40 días
            }
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

            bool existeFiniquito = estimaciones.Any(e => e.EsFiniquito);

            // Pasamos esta bandera a la vista
            ViewBag.YaExisteFiniquito = existeFiniquito;
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
        public async Task<IActionResult> CrearEstimacion(EstimacionCrearViewModel viewModel)
        {
            string valorCheckbox = Request.Form["CheckFiniquitoManual"];
            if (!string.IsNullOrEmpty(valorCheckbox) && valorCheckbox.Contains("true"))
            {
                viewModel.EsFiniquito = true;
            }
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
            var proyecto = await _context.Proyectos.FindAsync(viewModel.IdProyectoFk);

            if (proyecto == null)
            {
                TempData["Error"] = "El proyecto no existe.";
                return RedirectToAction("Index", "Proyectos"); // O a donde corresponda
            }

            // --- INICIO LÓGICA DE SLA (TAREA 2) ---

            // 1. Revisar si el proyecto está bloqueado
            if (proyecto.EstaBloqueado)
            {
                TempData["Error"] = "Este proyecto está bloqueado administrativamente y no puede recibir nuevas estimaciones. Contacte a Control de Obra.";
                return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { id = viewModel.IdProyectoFk });
            }

            // 2. Revisar si es la PRIMERA estimación que se intenta crear
            bool esPrimeraEstimacion = !await _context.Estimaciones.AnyAsync(e => e.IdProyectoFk == viewModel.IdProyectoFk);

            if (esPrimeraEstimacion)
            {
                // 3. Buscar la fecha de inicio de ejecución de la licitación (la "Toma 1")
                var licitacion = await _context.Licitaciones
                    .Where(l => l.ProyectoId == viewModel.IdProyectoFk && l.Estado == "Adjudicada")
                    .OrderByDescending(l => l.FechaFallo) // Tomar la más reciente
                    .FirstOrDefaultAsync();

                if (licitacion?.FechaInicioEjecucion != null)
                {
                    var fechaInicio = licitacion.FechaInicioEjecucion.Value;
                    var diasTranscurridos = (DateTime.Now - fechaInicio).TotalDays;

                    // 4. Aplicar bloqueo automático (30 días de SLA + 10 de gracia)
                    if (diasTranscurridos > 40)
                    {
                        proyecto.EstaBloqueado = true;
                        _context.Proyectos.Update(proyecto);

                        // 5. Registrar el bloqueo
                        _context.ProyectoHistorialBloqueo.Add(new ProyectoHistorialBloqueo
                        {
                            ProyectoId = proyecto.Id,
                            UsuarioId = usuarioActual.Id, // O un ID de "Sistema"
                            Accion = "Bloqueo Automático",
                            Comentario = $"Bloqueado por exceder 40 días sin la 1ra estimación. ({Math.Floor(diasTranscurridos)} días)"
                        });

                        await _context.SaveChangesAsync();

                        TempData["Error"] = "Proyecto bloqueado. Se superó el plazo de 40 días para registrar la primera estimación. Contacte a Control de Obra.";
                        return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { proyectoId = viewModel.IdProyectoFk });
                    }
                }
            }
            // 1. Crear la entidad principal (la Estimación)
            var estimacion = new Estimaciones
            {
                IdProyectoFk = viewModel.IdProyectoFk,
                Monto = viewModel.Monto,
                FechaEstimacion = viewModel.FechaEstimacion,
                Descripcion = viewModel.Descripcion,
                Estado = "En Revisión Control Obra",
                EsFiniquito = viewModel.EsFiniquito
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
                if (Request.Form.ContainsKey("CheckFiniquitoManual"))
                {
                    string valor = Request.Form["CheckFiniquitoManual"];
                    if (valor.Contains("true"))
                    {
                        viewModel.EsFiniquito = true;
                    }
                }
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
            
            return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { proyectoId = viewModel.IdProyectoFk });
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirDocumentoInterno(int estimacionId, string tipoDocumento, IFormFile archivo)
        {
            // 1. Validar que la estimación exista
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null)
            {
                return NotFound("La estimación no existe.");
            }

            // 2. Validar el archivo
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "No se seleccionó ningún archivo.";
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            // 3. Obtener el usuario que está subiendo el documento
            var usuarioActual = await _userManager.GetUserAsync(User);
            if (usuarioActual == null)
            {
                return Unauthorized(); // O redirigir a Login
            }

            try
            {
                // 4. Llamar al helper privado que ya tenías
                // (Este helper crea el registro del documento pero NO guarda cambios)
                await GuardarArchivoEstimacion(estimacionId, archivo, tipoDocumento, usuarioActual.Id);

                // 5. Guardar los cambios en la base de datos
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Documento '{tipoDocumento}' subido exitosamente.";
            }
            catch (Exception ex)
            {
                // Si algo falla (ej. permisos de carpeta), mostrar error
                TempData["Error"] = "Error al guardar el archivo: " + ex.Message;
            }

            // 6. Regresar al usuario a la misma página de detalles
            return RedirectToAction("Detalles", new { id = estimacionId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarATesoreria(int estimacionId)
        {
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();

            var usuarioActual = await _userManager.GetUserAsync(User);

            // --- Validación de Seguridad (Modificada) ---
            // Verificamos que los 3 documentos existan.
            var tieneFacturaPDF = await _context.EstimacionDocumentos
                .AnyAsync(d => d.EstimacionId == estimacionId && d.TipoDocumento == "Factura (PDF)");

            // --- LÍNEA MODIFICADA ---
            var tieneFacturaXLSX = await _context.EstimacionDocumentos
        .AnyAsync(d => d.EstimacionId == estimacionId && d.TipoDocumento == "Factura (XLSX)");// <-- CAMBIO DE XML A XLSX

            var tienePoliza = await _context.EstimacionDocumentos
                .AnyAsync(d => d.EstimacionId == estimacionId && d.TipoDocumento == "PolizaPago");

            // --- LÍNEA MODIFICADA ---
            if (!tieneFacturaPDF || !tieneFacturaXLSX || !tienePoliza) // <-- CAMBIO DE XML A XLSX
            {
                // --- LÍNEA MODIFICADA ---
                TempData["Error"] = "Faltan documentos (Factura PDF/XLSX o Póliza) para enviar a tesorería."; // <-- CAMBIO DE XML A XLSX
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            // 1. Cambiar estado
            string estadoAnterior = estimacion.Estado;
            estimacion.Estado = "En Trámite de Pago";

            // 2. Historial
            var historial = new EstimacionHistorial
            {
                EstimacionId = estimacion.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estimacion.Estado,
                UsuarioId = usuarioActual.Id,
                Comentario = "Expediente completo. Enviado a Tesorería para pago."
            };
            _context.EstimacionHistorial.Add(historial);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Estimación enviada a Tesorería.";
            return RedirectToAction("Detalles", new { id = estimacionId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarComoPagada(int estimacionId)
        {
            var estimacion = await _context.Estimaciones
                .Include(e => e.Proyecto)
                .FirstOrDefaultAsync(e => e.Id == estimacionId);

            if (estimacion == null) return NotFound();

            var usuarioActual = await _userManager.GetUserAsync(User);

            // 1. Cambiar estado
            string estadoAnterior = estimacion.Estado;
            estimacion.Estado = "Pagada";

            // 2. Historial
            var historial = new EstimacionHistorial
            {
                EstimacionId = estimacion.Id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estimacion.Estado,
                UsuarioId = usuarioActual.Id,
                Comentario = "Pago realizado por Tesorería. Estimación cerrada."
            };
            _context.EstimacionHistorial.Add(historial);

            // 3. Lógica de Automatización de Cierre
            if (estimacion.EsFiniquito)
            {
                const int FaseFinalizado = 6;
                var faseAnteriorId = estimacion.Proyecto.IdFaseFk;

                // Actualizamos el Proyecto a FINALIZADO
                estimacion.Proyecto.IdFaseFk = FaseFinalizado;
                estimacion.Proyecto.Estatus = "Finalizado";

                // Historial del Proyecto
                var historialFase = new HistorialFase
                {
                    ProyectoId = estimacion.Proyecto.Id,
                    FaseAnteriorId = faseAnteriorId,
                    FaseNuevaId = FaseFinalizado,
                    FechaCambio = DateTime.Now,
                    TipoCambio = "Aprobado",
                    UsuarioId = usuarioActual.Id,
                    Comentario = "Cierre Automático: Se pagó la estimación de Finiquito. Proyecto finalizado."
                };
                _context.HistorialFases.Add(historialFase);

                // Mensaje específico para Finiquito
                TempData["SuccessMessage"] = "Estimación pagada y Proyecto FINALIZADO exitosamente.";
            }
            else
            {
                // Mensaje normal
                TempData["SuccessMessage"] = "Estimación marcada como 'Pagada' exitosamente.";
            }

            await _context.SaveChangesAsync();

            // ¡YA NO PONEMOS NADA AQUÍ PARA NO SOBRESCRIBIR EL MENSAJE!
            return RedirectToAction("Detalles", new { id = estimacionId });
        }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Jefa,Admin")] // Solo Jefatura puede desbloquear
        public async Task<IActionResult> DesbloquearProyecto(string proyectoId, string comentario)
        {
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            if (proyecto == null) return NotFound();

            proyecto.EstaBloqueado = false;
            _context.Proyectos.Update(proyecto);

            var usuarioActual = await _userManager.GetUserAsync(User);
            _context.ProyectoHistorialBloqueo.Add(new ProyectoHistorialBloqueo
            {
                ProyectoId = proyecto.Id,
                UsuarioId = usuarioActual.Id,
                Accion = "Desbloqueo Manual",
                Comentario = comentario
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Proyecto desbloqueado exitosamente.";
            return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { id = proyectoId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Jefa,Admin,ControlObra")] // Quienes pueden hacer esto
        public async Task<IActionResult> TerminarObra(int estimacionId)
        {
            var estimacion = await _context.Estimaciones
                                            .Include(e => e.Proyecto)
                                            .FirstOrDefaultAsync(e => e.Id == estimacionId);

            if (estimacion == null || estimacion.Proyecto == null) return NotFound();

            // Validar que los documentos existan (seguridad)
            var documentos = await _context.EstimacionDocumentos
                .Where(d => d.EstimacionId == estimacionId)
                .Select(d => d.TipoDocumento)
                .ToListAsync();

            if (!documentos.Contains("Acta Entrega-Recepción") || !documentos.Contains("Acta Finiquito"))
            {
                TempData["Error"] = "Faltan documentos obligatorios (Acta de Entrega o Finiquito) para cerrar la obra.";
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            // Actualizar el estado del PROYECTO
            var proyecto = estimacion.Proyecto;
            proyecto.Estatus = "Finiquitado"; // O "Finalizado"
            _context.Proyectos.Update(proyecto);

            // (Opcional: puedes añadir un historial al proyecto también)

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"El proyecto '{proyecto.NombreProyecto}' ha sido marcado como 'Finiquitado' exitosamente.";
            return RedirectToAction("Detalles", new { id = estimacionId });
        }
    }
}