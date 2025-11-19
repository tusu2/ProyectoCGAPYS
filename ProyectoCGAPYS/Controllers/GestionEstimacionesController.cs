using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Data; // (Ajusta tu namespace de Data)
using ProyectoCGAPYS.Datos;
using ProyectoCGAPYS.Models; // (Ajusta tu namespace de Models)
using ProyectoCGAPYS.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace ProyectoCGAPYS.Controllers
{
  

    // Solo el personal interno (Jefa, Empleados) puede acceder aquí
    [Authorize(Roles = "Jefa,Empleado1,Empleado2,Empleado3")]
    public class GestionEstimacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public GestionEstimacionesController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment; // Añade esto
        }

        // GET: /GestionEstimaciones
        // Este será el "Dashboard de Tareas Pendientes"
        public async Task<IActionResult> Index()
        {
            // 1. Obtenemos solo los proyectos en Fase 5 (Ejecución)
            var proyectosEnEjecucion = await _context.Proyectos
                .Where(p => p.IdFaseFk == 5) // 5 = En Ejecución
                .ToListAsync();

            var viewModelList = new List<ProyectoConteoViewModel>();

            foreach (var proyecto in proyectosEnEjecucion)
            {
                // 2. Por cada proyecto, contamos sus estimaciones pendientes
                int conteo = await _context.Estimaciones
                    .CountAsync(e => e.IdProyectoFk == proyecto.Id &&
                                     e.Estado != "Pagada" &&
                                     e.Estado != "En Creación"); // No contamos rechazadas

                viewModelList.Add(new ProyectoConteoViewModel
                {
                    Proyecto = proyecto,
                    ConteoPendientes = conteo
                });
            }

            // 3. Enviamos la lista de proyectos (con sus conteos) a la vista
            return View(viewModelList.OrderByDescending(p => p.ConteoPendientes).ToList());
        }
        // ... (dentro de GestionEstimacionesController.cs)

        // GET: /GestionEstimaciones/Detalles/5
        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // 1. Cargamos la estimación que queremos revisar
            var estimacion = await _context.Estimaciones
                .Include(e => e.Proyecto)       // Para ver el nombre del proyecto
                .Include(e => e.Documentos)     // Para ver los archivos adjuntos
                .Include(e => e.Historial)      // Para ver la bitácora de cambios
                    .ThenInclude(h => h.Usuario) // Para ver *quién* hizo cada cambio
                .FirstOrDefaultAsync(e => e.Id == id);

            if (estimacion == null)
            {
                return NotFound("Estimación no encontrada.");
            }

            // 2. (Opcional pero recomendado) Buscamos quién es el contratista
            //    Basado en tu lógica, el contratista es el "Ganador" de la licitación de ese proyecto.
            var licitacionGanadora = await _context.Licitaciones
                .Include(l => l.ContratistaGanador)
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ProyectoId == estimacion.IdProyectoFk && l.ContratistaGanadorId != null);

            if (licitacionGanadora != null)
            {
                ViewBag.Contratista = licitacionGanadora.ContratistaGanador;
            }
            else
            {
                ViewBag.Contratista = null; // O un contratista genérico si no se encuentra
            }

            // 3. Enviamos el modelo de "Estimaciones" a la vista
            return View(estimacion);
        }

        // ... (dentro de GestionEstimacionesController.cs)

        // POST: /GestionEstimaciones/AprobarSupervisor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarSupervisor(int estimacionId)
        {
            // 1. Validar y encontrar la estimación
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null)
            {
                return NotFound();
            }

            // 2. Obtener el usuario que está haciendo la acción
            var usuario = await _userManager.GetUserAsync(User);

            // 3. Verificamos que esté en el estado correcto
            if (estimacion.Estado == "En Revisión Supervisor")
            {
                // 4. CAMBIAR EL ESTADO (El corazón de la máquina)
              
                estimacion.Estado = "En Revisión Control Obra";
                _context.Update(estimacion);

                var historial = new EstimacionHistorial
                {
                    EstimacionId = estimacionId,
                    EstadoAnterior = "En Revisión Supervisor",
                    EstadoNuevo = "En Revisión Control Obra",
                    Comentario = "Aprobado por Supervisor.",
                    UsuarioId = usuario.Id,
                    FechaCambio = DateTime.Now
                };
                _context.EstimacionHistorial.Add(historial);

                // 6. Guardar todo
                await _context.SaveChangesAsync();

                TempData["Success"] = "Estimación APROBADA y enviada a Control de Obra.";
            }
            else
            {
                TempData["Error"] = "Esta estimación ya no se encuentra en 'Revisión Supervisor'.";
            }

            // Devolvemos al dashboard
            return RedirectToAction("Index");
        }

        // POST: /GestionEstimaciones/RechazarSupervisor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarSupervisor(int estimacionId, string comentario)
        {
            if (string.IsNullOrEmpty(comentario))
            {
                TempData["Error"] = "El comentario es obligatorio para rechazar.";
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null)
            {
                return NotFound();
            }

            var usuario = await _userManager.GetUserAsync(User);

            if (estimacion.Estado == "En Revisión Supervisor")
            {
                // 4. CAMBIAR EL ESTADO (Devolvemos al contratista)
             
                estimacion.Estado = "En Creación";
                _context.Update(estimacion);

           
                var historial = new EstimacionHistorial
                {
                    EstimacionId = estimacionId,
                    EstadoAnterior = "En Revisión Supervisor",
                    EstadoNuevo = "En Creación", // (Opcional: podrías usar "Rechazado (Supervisor)")
                    Comentario = comentario, // ¡Guardamos el motivo!
                    UsuarioId = usuario.Id,
                    FechaCambio = DateTime.Now
                };
                _context.EstimacionHistorial.Add(historial);

                // 6. Guardar todo
                await _context.SaveChangesAsync();

                TempData["Success"] = "Estimación RECHAZADA y devuelta al contratista.";
            }
            else
            {
                TempData["Error"] = "Esta estimación ya no se encuentra en 'Revisión Supervisor'.";
            }

            return RedirectToAction("Index");
        }
        // Asegúrate de inyectar IWebHostEnvironment en el constructor
        private readonly IWebHostEnvironment _webHostEnvironment;

     
        // ... (Aquí van tus acciones Index, Detalles, AprobarSupervisor, RechazarSupervisor) ...


        // --- ACCIONES NUEVAS PARA VISTA 3 ---

        // POST: /GestionEstimaciones/AprobarControlObra
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AprobarControlObra(int estimacionId)
        {
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();
            var usuario = await _userManager.GetUserAsync(User);

            if (estimacion.Estado == "En Revisión Control Obra")
            {
                // CAMBIAR ESTADO (Paso 3 -> 4)
                estimacion.Estado = "Aprobado (Pendiente Factura)";
                _context.Update(estimacion);

                // REGISTRAR HISTORIAL
                _context.EstimacionHistorial.Add(new EstimacionHistorial
                {
                    EstimacionId = estimacionId,
                    EstadoAnterior = "En Revisión Control Obra",
                    EstadoNuevo = "Aprobado (Pendiente Factura)",
                    Comentario = "Aprobado por Control de Obra. Se notifica a Contratista para subir factura.",
                    UsuarioId = usuario.Id,
                    FechaCambio = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Estimación APROBADA. Esperando Factura y Póliza.";
            }
            else
            {
                TempData["Error"] = "Esta estimación ya no se encuentra en 'Revisión Control Obra'.";
            }

            // Volvemos a la misma página de detalles
            return RedirectToAction("Detalles", new { id = estimacionId });
        }

        // POST: /GestionEstimaciones/RechazarControlObra
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RechazarControlObra(int estimacionId, string comentario)
        {
            if (string.IsNullOrEmpty(comentario))
            {
                TempData["Error"] = "El comentario es obligatorio para rechazar.";
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();
            var usuario = await _userManager.GetUserAsync(User);

            if (estimacion.Estado == "En Revisión Control Obra")
            {
              
                estimacion.Estado = "En Creación";
                _context.Update(estimacion);

                // REGISTRAR HISTORIAL
                _context.EstimacionHistorial.Add(new EstimacionHistorial
                {
                    EstimacionId = estimacionId,
                    EstadoAnterior = "En Revisión Control Obra",
                    EstadoNuevo = "En Creación",
                    Comentario = comentario, // Motivo del rechazo
                    UsuarioId = usuario.Id,
                    FechaCambio = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Estimación RECHAZADA y devuelta al contratista.";
            }
            else
            {
                TempData["Error"] = "Esta estimación ya no se encuentra en 'Revisión Control Obra'.";
            }

            return RedirectToAction("Index");
        }

        // POST: /GestionEstimaciones/SubirDocumentoInterno
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirDocumentoInterno(int estimacionId, string tipoDocumento, IFormFile archivo)
        {
            if (archivo == null || archivo.Length == 0)
            {
                TempData["Error"] = "No se seleccionó ningún archivo.";
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();
            var usuario = await _userManager.GetUserAsync(User);

            // Guardar el archivo (usando la misma lógica que el ContratistaController)
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "estimaciones");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(archivo.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(fileStream);
            }

            // Crear el registro en la BD
            var documento = new EstimacionDocumentos
            {
                EstimacionId = estimacionId,
                TipoDocumento = tipoDocumento, // "Factura" o "PolizaPago"
                NombreArchivo = Path.GetFileName(archivo.FileName),
                RutaArchivo = "/uploads/estimaciones/" + uniqueFileName,
                UsuarioId = usuario.Id,
                FechaSubida = DateTime.Now
            };

            _context.EstimacionDocumentos.Add(documento);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Documento '{tipoDocumento}' subido correctamente.";
            return RedirectToAction("Detalles", new { id = estimacionId });
        }

        // POST: /GestionEstimaciones/EnviarATesoreria
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnviarATesoreria(int estimacionId)
        {
            var estimacion = await _context.Estimaciones
                .Include(e => e.Documentos) // Necesitamos cargar los documentos para validar
                .FirstOrDefaultAsync(e => e.Id == estimacionId);

            if (estimacion == null) return NotFound();
            var usuario = await _userManager.GetUserAsync(User);


            bool tieneFactura = estimacion.Documentos.Any(d => d.TipoDocumento == "Factura");
            bool tienePoliza = estimacion.Documentos.Any(d => d.TipoDocumento == "PolizaPago");

            if (!tieneFactura || !tienePoliza)
            {
                TempData["Error"] = "Falta la Factura o la Póliza de Pago. No se puede enviar.";
                return RedirectToAction("Detalles", new { id = estimacionId });
            }

            if (estimacion.Estado == "Aprobado (Pendiente Factura)")
            {
                // CAMBIAR ESTADO (Paso 4 -> 5)
                estimacion.Estado = "En Trámite de Pago";
                _context.Update(estimacion);

                // REGISTRAR HISTORIAL
                _context.EstimacionHistorial.Add(new EstimacionHistorial
                {
                    EstimacionId = estimacionId,
                    EstadoAnterior = "Aprobado (Pendiente Factura)",
                    EstadoNuevo = "En Trámite de Pago",
                    Comentario = "Expediente completo. Enviado a Tesorería.",
                    UsuarioId = usuario.Id,
                    FechaCambio = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Estimación enviada a Tesorería para pago.";
            }
            else
            {
                TempData["Error"] = "Esta estimación no está lista para enviarse a Tesorería.";
            }

            // Volvemos al dashboard principal
            return RedirectToAction("Index");
        }

        // ... (después de tu acción EnviarATesoreria) ...

        // POST: /GestionEstimaciones/MarcarComoPagada
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Según el plan, solo Tesorería (Empleado3) y Jefa pueden pagar
        [Authorize(Roles = "Jefa,Empleado3")]
        public async Task<IActionResult> MarcarComoPagada(int estimacionId)
        {
            var estimacion = await _context.Estimaciones.FindAsync(estimacionId);
            if (estimacion == null) return NotFound();

            var usuario = await _userManager.GetUserAsync(User);

            if (estimacion.Estado == "En Trámite de Pago")
            {
                // CAMBIAR ESTADO (Paso 5 -> 6, Final)
                estimacion.Estado = "Pagada";
                _context.Update(estimacion);

                // REGISTRAR HISTORIAL (El registro final)
                _context.EstimacionHistorial.Add(new EstimacionHistorial
                {
                    EstimacionId = estimacionId,
                    EstadoAnterior = "En Trámite de Pago",
                    EstadoNuevo = "Pagada",
                    Comentario = "Pago realizado por Tesorería.",
                    UsuarioId = usuario.Id,
                    FechaCambio = DateTime.Now
                });

                await _context.SaveChangesAsync();
                TempData["Success"] = "Estimación marcada como PAGADA. El flujo ha terminado.";
            }
            else
            {
                TempData["Error"] = "Esta estimación no se encuentra en 'Trámite de Pago'.";
            }

            // Volvemos al dashboard principal
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> DashboardPorProyecto(string proyectoId) // Recibe el ID del proyecto
        {
            if (proyectoId == null) return NotFound();

            // 1. Obtener el proyecto para sacar el nombre y folio
            var proyecto = await _context.Proyectos.FindAsync(proyectoId);
            ViewBag.EstaBloqueado = proyecto.EstaBloqueado;
            ViewBag.SLAStatus = "N/A"; // (OK, Advertencia, Vencido)
            ViewBag.SLADias = 0;

            var licitacion = await _context.Licitaciones
    .Where(l => l.ProyectoId == proyectoId && l.Estado == "Adjudicada")
    .OrderByDescending(l => l.FechaFallo)
    .FirstOrDefaultAsync();

            bool tienePrimeraEstimacion = await _context.Estimaciones.AnyAsync(e => e.IdProyectoFk == proyectoId);
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
                .Where(e => e.IdProyectoFk == proyectoId)
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
            var estimacionesProyecto = await _context.Estimaciones
            .Include(e => e.Proyecto)
            .Where(e => e.IdProyectoFk == proyectoId)
            .OrderByDescending(e => e.FechaEstimacion)
            .ToListAsync();
            var viewModel = estimacionesProyecto
             .GroupBy(e => e.Estado)
             .ToDictionary(g => g.Key, g => g.ToList());
            return View(viewModel);
        }
      
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEstimacion(EstimacionCrearViewModel viewModel)
        {
            // Validar que venga el ID del proyecto
            if (string.IsNullOrEmpty(viewModel.IdProyectoFk))
            {
              ModelState.AddModelError("IdProyectoFk", "El ID del proyecto es requerido.");
    
            }

            // Limpiamos validaciones del DropDown que ya no usamos aquí si viene nulo

            ModelState.Remove("ProyectosAsignados");
            ModelState.Remove("NuevaEstimacion");
            if (!ModelState.IsValid)
            {
                // TRUCO DE DEBUG: Esto imprimirá en tu consola de Visual Studio exactamente qué campo está fallando
                foreach (var state in ModelState)
                {
                    foreach (var error in state.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR EN CAMPO: {state.Key} - MENSAJE: {error.ErrorMessage}");
                    }
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["Error"] = "Datos inválidos: " + string.Join("; ", errors);

                return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { proyectoId = viewModel.IdProyectoFk });
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
            return RedirectToAction("DashboardPorProyecto", "GestionEstimaciones", new { proyectoId = viewModel.IdProyectoFk });
        }

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
