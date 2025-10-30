using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoCGAPYS.Data; // Reemplaza por tu namespace de DbContext
using ProyectoCGAPYS.Models; // Reemplaza por tu namespace de Models
using ProyectoCGAPYS.ViewModels; // Reemplaza por tu namespace de ViewModels
using System.Linq;
using System.Threading.Tasks;
using ProyectoCGAPYS.Datos;
using static ProyectoCGAPYS.ViewModels.LicitacionDetalleViewModel;
using Microsoft.AspNetCore.Identity;

// Solo los usuarios autorizados (administradores/empleados) podrán acceder a este controlador.
[Authorize(Roles = "Jefa,Empleado1,Empleado2,Empleado3")]
public class LicitacionesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly bool _habilitarModoGestion = false;

    // Inyectamos el DbContext para poder interactuar con la base de datos.
    public LicitacionesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    // En LicitacionesController.cs


    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        if (userId != null)
        {
            var notificacionesSinLeer = await _context.Notificaciones
                .Where(n => n.UsuarioId == userId && !n.Leida && n.Url.Contains("/Licitaciones"))
                .OrderByDescending(n => n.FechaCreacion)
                .ToListAsync();

            if (notificacionesSinLeer.Any())
            {
                // --- LÍNEA AÑADIDA ---
                // Le "avisamos" al Layout cuál era el conteo ANTES de limpiar.
                ViewBag.ConteoNotificacionesBadge = notificacionesSinLeer.Count;

                ViewBag.NuevasNotificaciones = notificacionesSinLeer;

                foreach (var notificacion in notificacionesSinLeer)
                {
                    notificacion.Leida = true;
                }
                await _context.SaveChangesAsync();
            }
        }

        var licitaciones = await _context.Licitaciones
                                         .Include(l => l.Proyecto)
                                         .OrderByDescending(l => l.FechaInicio)
                                         .ToListAsync();
        return View(licitaciones);
    }

    // GET: /Licitaciones/Crear?proyectoId=PROY-001
    // Muestra el formulario para crear una nueva licitación asociada a un proyecto.
    public async Task<IActionResult> Crear(string proyectoId)
    {
        if (string.IsNullOrEmpty(proyectoId))
        {
            return BadRequest("El ID del proyecto es necesario.");
        }

        var proyecto = await _context.Proyectos.FindAsync(proyectoId);
        if (proyecto == null)
        {
            return NotFound("El proyecto especificado no existe.");
        }

        // Creamos el ViewModel y le pasamos la información del proyecto.
        var viewModel = new CrearLicitacionViewModel
        {
            ProyectoId = proyecto.Id,
            ProyectoNombre = proyecto.NombreProyecto,
            FechaInicio = DateTime.Today // Pre-llenamos la fecha de inicio.
        };

        // --- LÍNEA AÑADIDA ---
        // Le pasamos el interruptor a la vista
        ViewBag.ModoGestionHabilitado = _habilitarModoGestion;
        // -------------------------

        return View(viewModel);
    }

    // POST: /Licitaciones/Crear
    // Procesa los datos enviados desde el formulario de creación.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(CrearLicitacionViewModel viewModel)
    {
        // Verificamos si el modelo es válido según las reglas que definimos (ej. [Required]).
        if (ModelState.IsValid)
        {
            // Creamos una nueva entidad Licitacion con los datos del ViewModel.
            var nuevaLicitacion = new Licitacion
            {
                ProyectoId = viewModel.ProyectoId,
                NumeroLicitacion = viewModel.NumeroLicitacion,
                Descripcion = viewModel.Descripcion,
                FechaInicio = viewModel.FechaInicio,
                FechaFinPropuestas = viewModel.FechaFinPropuestas,
                Estado = "Preparacion" // Estado inicial por defecto.
            };

            _context.Licitaciones.Add(nuevaLicitacion);
            await _context.SaveChangesAsync(); // Guardamos los cambios en la BD.

            // Redirigimos al usuario a la página de detalles de la licitación recién creada.
            // (Crearemos esta vista en el siguiente paso).
            return RedirectToAction("Detalles", new { id = nuevaLicitacion.Id });
        }

        // Si el modelo no es válido, volvemos a mostrar el formulario con los errores.
        // Recargamos el nombre del proyecto, ya que no se envía de vuelta con el POST.
        var proyecto = await _context.Proyectos.FindAsync(viewModel.ProyectoId);
        viewModel.ProyectoNombre = proyecto?.NombreProyecto;

        return View(viewModel);
    }
    public async Task<IActionResult> DetallesPorProyecto(string proyectoId)
    {
        if (string.IsNullOrEmpty(proyectoId))
        {
            return BadRequest();
        }

        var licitacion = await _context.Licitaciones
                                       .FirstOrDefaultAsync(l => l.ProyectoId == proyectoId);

        if (licitacion == null)
        {
            // Opcional: Podrías redirigir a la página de Crear Licitación si no existe una.
            return NotFound("No se encontró una licitación para el proyecto especificado.");
        }

        return RedirectToAction("Detalles", new { id = licitacion.Id });
    }


    public async Task<IActionResult> Detalles(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        // --- LÓGICA DE INTERRUPTOR AÑADIDA ---
        // (Prevenimos que se intente adjudicar una licitación de Gestión si el modo está apagado)
        var licitacionParaValidar = await _context.Licitaciones.FindAsync(id);
        if (licitacionParaValidar != null &&
            licitacionParaValidar.Estado == "Adjudicada" &&
            licitacionParaValidar.TipoProceso == "Licitación Pública" && // Es de gestión
            !_habilitarModoGestion) // Y el modo gestión está apagado
        {
            TempData["Info"] = "No se puede acceder a una licitación adjudicada de este tipo mientras el modo de gestión esté deshabilitado.";
            return RedirectToAction("Index");
        }
        // (Tu validación anterior era buena, pero la adaptamos)
        if (licitacionParaValidar != null &&
            licitacionParaValidar.Estado == "Adjudicada" &&
            licitacionParaValidar.TipoProceso == "Adjudicación Directa") // Es de control
        {
            TempData["Info"] = "Esta licitación ya fue adjudicada y enviada a Ejecución.";
            // Podrías redirigir al Index o a la página del Proyecto.
            // Por ahora, lo dejamos pasar para que vean los detalles (solo lectura).
            // return RedirectToAction("Index"); 
        }

        // --- CONSULTA PRINCIPAL MODIFICADA ---
        var licitacion = await _context.Licitaciones
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.Campus)
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.Dependencia)
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.TipoFondo)
             .Include(l => l.Proyecto)
                 .ThenInclude(p => p.Documentos)
             .Include(l => l.ContratistasParticipantes) // (Se mantiene para Modo Gestión)
                 .ThenInclude(pc => pc.Contratista)
             // --- NUEVO ---
             .Include(l => l.ContratistaGanador) // Cargamos al ganador único
                                                 // --- NUEVO ---
             .Include(l => l.LicitacionDocumentos) // Cargamos los nuevos documentos
             .FirstOrDefaultAsync(m => m.Id == id);

        if (licitacion == null)
        {
            return NotFound();
        }

        // --- LÍNEA AÑADIDA ---
        // Pasamos el interruptor a la vista
        ViewBag.ModoGestionHabilitado = _habilitarModoGestion;
        // -------------------------

        // Ahora, llenamos nuestro ViewModel con los datos que obtuvimos.
        var viewModel = new LicitacionDetalleViewModel
        {
            LicitacionId = licitacion.Id,
            ProyectoId = licitacion.ProyectoId,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            DescripcionLicitacion = licitacion.Descripcion,
            Estado = licitacion.Estado,
            FechaInicio = licitacion.FechaInicio,
            FechaFinPropuestas = licitacion.FechaFinPropuestas,
            Latitud = licitacion.Proyecto.Latitud,
            Longitud = licitacion.Proyecto.Longitud,
            FolioProyecto = licitacion.Proyecto.Folio,
            PresupuestoProyecto = licitacion.Proyecto.Presupuesto,
            CampusNombre = licitacion.Proyecto.Campus?.Nombre,
            DependenciaNombre = licitacion.Proyecto.Dependencia?.Nombre,
            TipoFondoNombre = licitacion.Proyecto.TipoFondo?.Nombre,

            // --- PROPIEDADES NUEVAS ASIGNADAS ---
            TipoProceso = licitacion.TipoProceso,
            FechaFallo = licitacion.FechaFallo,
            NumeroContrato = licitacion.NumeroContrato,

            // Asignamos al ganador (si existe)
            ContratistaGanador = licitacion.ContratistaGanador == null ? null : new ContratistaViewModel
            {
                Id = licitacion.ContratistaGanador.Id,
                RazonSocial = licitacion.ContratistaGanador.RazonSocial,
                RFC = licitacion.ContratistaGanador.RFC
            },

            // Asignamos los nuevos documentos de la licitación
            LicitacionDocumentos = licitacion.LicitacionDocumentos.Select(d => new LicitacionDocumentoViewModel
            {
                Id = d.Id,
                TipoDocumento = d.TipoDocumento,
                NombreArchivo = d.NombreArchivo,
                RutaArchivo = d.RutaArchivo,
                FechaSubida = d.FechaSubida
            }).ToList(),

            // --- PROPIEDADES EXISTENTES (Se mantienen) ---
            Participantes = licitacion.ContratistasParticipantes.Select(p => new ParticipanteViewModel
            {
                ContratistaId = p.ContratistaId,
                RazonSocial = p.Contratista.RazonSocial,
                RFC = p.Contratista.RFC,
                EstadoParticipacion = p.EstadoParticipacion,
                FechaInvitacion = p.FechaInvitacion
            }).ToList(),

            Documentos = licitacion.Proyecto.Documentos.Select(d => new DocumentoViewModel
            {
                NombreArchivo = d.NombreArchivo,
                RutaArchivo = d.RutaArchivo
            }).ToList()
        };

        return View(viewModel);
    }
    // GET: Licitaciones/Invitar/5
    public async Task<IActionResult> Invitar(int id)
    {
        var licitacion = await _context.Licitaciones
                                       .Include(l => l.Proyecto)
                                       .FirstOrDefaultAsync(l => l.Id == id);
        if (licitacion == null) return NotFound();

        // Obtenemos los IDs de los contratistas que ya están invitados a esta licitación.
        var idsContratistasYaInvitados = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == id)
            .Select(lc => lc.ContratistaId)
            .ToListAsync();

        // Preparamos el ViewModel
        var viewModel = new InvitarContratistaViewModel
        {
            LicitacionId = licitacion.Id,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            // Obtenemos todos los contratistas y marcamos los que ya fueron invitados.
            ContratistasDisponibles = await _context.Contratistas
    .Where(c => !idsContratistasYaInvitados.Contains(c.Id)) // <-- LÍNEA CLAVE
    .Select(c => new ContratistaSeleccionable
    {
        Id = c.Id,
        RazonSocial = c.RazonSocial,
        RFC = c.RFC,
        YaEstaInvitado = false // Esto ya no es necesario, pero lo dejamos por consistencia
    })
    .OrderBy(c => c.RazonSocial) // Es bueno ordenar la lista
    .ToListAsync()
        };

        return View(viewModel);
    }


    // POST: Licitaciones/Invitar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invitar(int licitacionId, List<int> contratistasSeleccionados)
    {
        if (contratistasSeleccionados == null || !contratistasSeleccionados.Any())
        {
            TempData["Error"] = "No se seleccionó ningún contratista.";
            return RedirectToAction("Invitar", new { id = licitacionId });
        }

        // Obtenemos las invitaciones que ya existen para no duplicarlas.
        var idsContratistasYaInvitados = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == licitacionId)
            .Select(lc => lc.ContratistaId)
            .ToListAsync();

        foreach (var contratistaId in contratistasSeleccionados)
        {
            // Solo agregamos la invitación si el contratista no está ya en la lista.
            if (!idsContratistasYaInvitados.Contains(contratistaId))
            {
                var nuevaInvitacion = new LicitacionContratista
                {
                    LicitacionId = licitacionId,
                    ContratistaId = contratistaId,
                    FechaInvitacion = DateTime.Now,
                    EstadoParticipacion = "Invitado" // Estado inicial
                };
                _context.LicitacionContratistas.Add(nuevaInvitacion);
            }
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Contratistas invitados correctamente.";

        // Devolvemos al usuario a la página de detalles para que vea la lista actualizada.
        return RedirectToAction("Detalles", new { id = licitacionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarFechas(int licitacionId, DateTime fechaInicio, DateTime? fechaFinPropuestas)
    {
        var licitacion = await _context.Licitaciones.FindAsync(licitacionId);

        if (licitacion == null)
        {
            return NotFound();
        }

        // Actualizamos las propiedades de la licitación con los valores del formulario
        licitacion.FechaInicio = fechaInicio;
        licitacion.FechaFinPropuestas = fechaFinPropuestas;

        _context.Update(licitacion);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Las fechas de la licitación se han actualizado correctamente.";

        return RedirectToAction("Detalles", new { id = licitacionId });
    }
    // GET: Licitaciones/_InvitarContratistasPartial/5?busqueda=...
    // Esta acción devuelve la vista parcial con la lista de contratistas para el modal.
    public async Task<IActionResult> _InvitarContratistasPartial(int id, string busqueda)
    {
        var licitacion = await _context.Licitaciones
                                       .Include(l => l.Proyecto)
                                       .FirstOrDefaultAsync(l => l.Id == id);
        if (licitacion == null) return NotFound();

        var idsContratistasYaInvitados = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == id)
            .Select(lc => lc.ContratistaId)
            .ToListAsync();

        // Empezamos la consulta de contratistas
        var query = _context.Contratistas.AsQueryable();

        // Si hay un término de búsqueda, lo aplicamos
        if (!string.IsNullOrEmpty(busqueda))
        {
            query = query.Where(c => c.RazonSocial.Contains(busqueda) || c.RFC.Contains(busqueda));
        }

        var viewModel = new InvitarContratistaViewModel
        {
            LicitacionId = licitacion.Id,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            ContratistasDisponibles = await query
    .Where(c => !idsContratistasYaInvitados.Contains(c.Id)) // <-- MISMA LÍNEA CLAVE
    .Select(c => new ContratistaSeleccionable
    {
        Id = c.Id,
        RazonSocial = c.RazonSocial,
        RFC = c.RFC,
        YaEstaInvitado = false
    })
    .OrderBy(c => c.RazonSocial)
    .ToListAsync()
        };

        return PartialView("_InvitarContratistasPartial", viewModel);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevolverFase(int licitacionId, string comentario)
    {
        const int fasePresupuestoId = 3;
        const int faseLicitacionId = 4;

        var licitacion = await _context.Licitaciones
                                       .Include(l => l.Proyecto)
                                       .FirstOrDefaultAsync(l => l.Id == licitacionId);

        if (licitacion == null || licitacion.Proyecto == null)
        {
            return NotFound();
        }

        var proyecto = licitacion.Proyecto;
        var faseActualId = proyecto.IdFaseFk;

        if (proyecto.IdFaseFk != faseLicitacionId)
        {
            TempData["Error"] = "Esta acción solo es válida para proyectos en la fase 'En Licitación'.";
            return RedirectToAction("Detalles", new { id = licitacionId });
        }

        // --- BLOQUE NUEVO ---
        // 1. Buscamos y eliminamos primero a los "hijos" (las invitaciones)
        var invitaciones = await _context.LicitacionContratistas
            .Where(lc => lc.LicitacionId == licitacion.Id)
            .ToListAsync();

        if (invitaciones.Any())
        {
            _context.LicitacionContratistas.RemoveRange(invitaciones);
        }
        // -------------------

        // 2. Ahora sí, eliminamos al "padre" (la licitación)
        _context.Licitaciones.Remove(licitacion);

        // 3. Actualizamos el proyecto a la fase anterior
        proyecto.IdFaseFk = fasePresupuestoId;
        _context.Proyectos.Update(proyecto);

        // 4. Registrar el cambio en el historial
        var historial = new HistorialFase
        {
            ProyectoId = proyecto.Id,
            FaseAnteriorId = faseActualId,
            FaseNuevaId = fasePresupuestoId,
            FechaCambio = DateTime.Now,
            Comentario = comentario,
            TipoCambio = "Devuelto"
        };
        _context.HistorialFases.Add(historial);

        // 5. Guardar todos los cambios en la base de datos
        await _context.SaveChangesAsync();

        TempData["Success"] = $"El proyecto '{proyecto.NombreProyecto}' ha sido devuelto a la fase de presupuesto y la licitación ha sido eliminada.";

        return RedirectToAction("Index", "PanelDeFases");
    }
    // POST: Licitaciones/Activar/5
    // Esta acción se encarga de cambiar el estado de la licitación a "Activo".
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activar(int id)
    {
        var licitacion = await _context.Licitaciones.FindAsync(id);

        if (licitacion == null) return NotFound();

        if (licitacion.Estado == "Preparacion")
        {
            licitacion.Estado = "Activo";
            // Usamos la variable corregida _userManager
            licitacion.UsuarioIdActivacion = _userManager.GetUserId(User);

            _context.Update(licitacion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "La licitación ha sido activada.";
        }
        else
        {
            TempData["Error"] = "Esta licitación no se puede activar.";
        }

        return RedirectToAction("Detalles", new { id = id });
    }

    // GET: Licitaciones/VerPropuestas/5
    // Esta acción prepara los datos para la nueva página que muestra todas las propuestas.
    public async Task<IActionResult> VerPropuestas(int id)
    {
        var licitacion = await _context.Licitaciones
                                        .Include(l => l.Proyecto)
                                        .FirstOrDefaultAsync(l => l.Id == id);

        if (licitacion == null)
        {
            return NotFound();
        }

        var viewModel = new VerPropuestasViewModel
        {
            LicitacionId = licitacion.Id,
            NumeroLicitacion = licitacion.NumeroLicitacion,
            ProyectoNombre = licitacion.Proyecto.NombreProyecto,
            EstadoLicitacion = licitacion.Estado,
            Contratistas = await _context.LicitacionContratistas
                .Where(lc => lc.LicitacionId == id)
                .Include(lc => lc.Contratista)
                .Select(lc => new ContratistaConPropuestasViewModel
                {
                    // --- LÍNEA CORREGIDA ---
                    ContratistaId = lc.Contratista.Id, // <-- ¡Aquí estaba el error! Mapeamos el Id del modelo.

                    RazonSocial = lc.Contratista.RazonSocial,
                    RFC = lc.Contratista.RFC,
                    Propuestas = _context.PropuestasContratistas
                                            .Where(p => p.LicitacionId == id && p.ContratistaId == lc.ContratistaId)
                                            .Select(p => new PropuestaResumenViewModel
                                            {
                                                NombreArchivo = p.NombreArchivo,
                                                RutaArchivo = p.RutaArchivo,
                                                Descripcion = p.Descripcion,
                                                FechaSubida = p.FechaSubida
                                            }).ToList()
                }).ToListAsync()
        };

        return View(viewModel);
    }
    // POST: Licitaciones/Cerrar/5
    // Esta acción cambia el estado de una licitación Activa a Cerrada.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var licitacion = await _context.Licitaciones.FindAsync(id);

        if (licitacion == null)
        {
            return NotFound();
        }

        // Solo se puede cerrar si el estado actual es "Activo".
        if (licitacion.Estado == "Activo")
        {
            licitacion.Estado = "Cerrado";
            _context.Update(licitacion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "La licitación ha sido cerrada. Ya no se aceptan más propuestas.";
        }
        else
        {
            TempData["Error"] = "Esta licitación no se puede cerrar porque no está activa.";
        }

        return RedirectToAction("Detalles", new { id = id });
    }

    // POST: Licitaciones/QuitarInvitacion
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarInvitacion(int licitacionId, int contratistaId)
    {
        // Buscamos la licitación para asegurarnos de que está en el estado correcto.
        var licitacion = await _context.Licitaciones.FindAsync(licitacionId);

        // Solo permitimos quitar contratistas si la licitación está en "Preparacion".
        if (licitacion == null || licitacion.Estado != "Preparacion")
        {
            TempData["Error"] = "No se puede quitar a un contratista de una licitación que no está en preparación.";
            return RedirectToAction("Detalles", new { id = licitacionId });
        }

        // Buscamos la invitación específica que queremos eliminar.
        var invitacion = await _context.LicitacionContratistas
            .FirstOrDefaultAsync(lc => lc.LicitacionId == licitacionId && lc.ContratistaId == contratistaId);

        if (invitacion != null)
        {
            _context.LicitacionContratistas.Remove(invitacion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Contratista quitado de la licitación correctamente.";
        }

        return RedirectToAction("Detalles", new { id = licitacionId });
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjudicarGanador(int licitacionId, int contratistaGanadorId)
    {
        // IDs de las fases para mayor claridad. Búscalos en tu tabla [Fases]
        const int faseLicitacionId = 4;
        const int faseEjecucionId = 5;

        // Usamos una transacción para asegurar que todas las operaciones se completen con éxito o ninguna lo haga.
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var licitacion = await _context.Licitaciones
                                           .Include(l => l.Proyecto)
                                           .FirstOrDefaultAsync(l => l.Id == licitacionId);

            if (licitacion == null)
            {
                return NotFound("Licitación no encontrada.");
            }

            // 1. Validar que el proyecto y la licitación estén en el estado correcto
            if (licitacion.Proyecto.IdFaseFk != faseLicitacionId)
            {
                TempData["Error"] = "El proyecto no se encuentra en la fase de licitación.";
                return RedirectToAction("Detalles", new { id = licitacionId });
            }
            if (licitacion.Estado != "Cerrado") // Idealmente, solo adjudicar licitaciones cerradas.
            {
                TempData["Error"] = "La licitación debe estar en estado 'Cerrado' para poder adjudicar un ganador.";
                return RedirectToAction("Detalles", new { id = licitacionId });
            }
            if (licitacion.Estado != "Cerrado")
            {
                TempData["Error"] = "Esta acción no se puede realizar. La licitación debe estar en estado 'Cerrado' para poder adjudicar un ganador.";
                // Lo devolvemos a la página de propuestas.
                return RedirectToAction("VerPropuestas", new { id = licitacionId });
            }
            // 2. Actualizar el estado de la licitación
            licitacion.Estado = "Adjudicada";
            _context.Licitaciones.Update(licitacion);

            // 3. Actualizar el estado de todos los participantes
            var participantes = await _context.LicitacionContratistas
            // --- NECESITAMOS INCLUIR AL CONTRATISTA PARA OBTENER SU USERID ---
            .Include(lc => lc.Contratista)
            .Where(lc => lc.LicitacionId == licitacionId)
            .ToListAsync();
            foreach (var participante in participantes)
            {
                // --- CÓDIGO AÑADIDO PARA NOTIFICAR ---
                var notificacion = new Notificacion
                {
                    UsuarioId = participante.Contratista.UsuarioId,
                    Url = "/Contratista/DetallesLicitacion/" + licitacionId,
                    FechaCreacion = DateTime.Now,
                    Leida = false
                };

                if (participante.ContratistaId == contratistaGanadorId)
                {
                    participante.EstadoParticipacion = "Ganador";
                    // Mensaje para el ganador
                    notificacion.Mensaje = $"¡Felicidades! Has sido adjudicado como ganador de la licitación '{licitacion.NumeroLicitacion}'.";
                }
                else
                {
                    participante.EstadoParticipacion = "No Seleccionado";
                    // Mensaje para los demás
                    notificacion.Mensaje = $"El proceso para la licitación '{licitacion.NumeroLicitacion}' ha concluido. Agradecemos tu participación.";
                }

                _context.Notificaciones.Add(notificacion);
                // --- FIN DEL CÓDIGO AÑADIDO ---
            }
            _context.LicitacionContratistas.UpdateRange(participantes);

            // 4. Mover el proyecto a la siguiente fase
            var proyecto = licitacion.Proyecto;
            proyecto.IdFaseFk = faseEjecucionId;
            _context.Proyectos.Update(proyecto);

            // 5. Registrar el cambio en el historial
            var historial = new HistorialFase
            {
                ProyectoId = proyecto.Id,
                FaseAnteriorId = faseLicitacionId,
                FaseNuevaId = faseEjecucionId,
                FechaCambio = DateTime.Now,
                Comentario = "Licitación adjudicada. El proyecto avanza a la fase de ejecución.",
                TipoCambio = "Aprobado"
                // Opcional: podrías guardar el ID del usuario que realizó la acción
                // UsuarioId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            };
            _context.HistorialFases.Add(historial);

            // 6. Guardar todos los cambios en la base de datos
            await _context.SaveChangesAsync();

            // 7. Si todo salió bien, confirmar la transacción
            await transaction.CommitAsync();

            TempData["Success"] = $"El proyecto '{proyecto.NombreProyecto}' ha sido adjudicado y movido a la fase de 'Ejecución'.";

            // Redirigir al panel de fases para ver el proyecto en su nueva columna
            return RedirectToAction("Index", "PanelDeFases");
        }
        catch (Exception ex)
        {
            // Si algo falla, revertimos todos los cambios.
            await transaction.RollbackAsync();
            // Opcional: Registrar el error 'ex' en un sistema de logs.
            TempData["Error"] = "Ocurrió un error al intentar adjudicar la licitación. La operación ha sido revertida.";
            return RedirectToAction("VerPropuestas", new { id = licitacionId });
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarDatosControl(int licitacionId, DateTime? fechaFallo, string numeroContrato)
    {
        var licitacion = await _context.Licitaciones.FindAsync(licitacionId);
        if (licitacion == null) return NotFound();

        // Solo actualizamos si no está adjudicada
        if (licitacion.Estado != "Adjudicada")
        {
            licitacion.FechaFallo = fechaFallo;
            licitacion.NumeroContrato = numeroContrato;
            _context.Update(licitacion);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Datos de registro actualizados.";
        }
        else
        {
            TempData["Error"] = "No se puede modificar una licitación ya adjudicada.";
        }

        return RedirectToAction("Detalles", new { id = licitacionId });
    }

    // [POST] Sube un documento (Fallo, Contrato, Fianza)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubirDocumentoLicitacion(int licitacionId, string tipoDocumento, IFormFile archivo)
    {
        var licitacion = await _context.Licitaciones.FindAsync(licitacionId);
        if (licitacion == null) return NotFound();
        if (licitacion.Estado == "Adjudicada")
        {
            TempData["Error"] = "No se pueden subir documentos a una licitación ya adjudicada.";
            return RedirectToAction("Detalles", new { id = licitacionId });
        }

        if (archivo != null && archivo.Length > 0)
        {
            // 1. Definir la ruta (¡Asegúrate de que esta carpeta exista!)
            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "licitaciones");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + archivo.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 2. Guardar el archivo en el servidor
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await archivo.CopyToAsync(fileStream);
            }

            // 3. Crear el registro en la base de datos
            var documento = new LicitacionDocumento
            {
                LicitacionId = licitacionId,
                TipoDocumento = tipoDocumento,
                NombreArchivo = archivo.FileName,
                RutaArchivo = "/uploads/licitaciones/" + uniqueFileName, // Ruta web
                FechaSubida = DateTime.Now
            };

            _context.LicitacionDocumentos.Add(documento);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Documento subido correctamente.";
        }
        else
        {
            TempData["Error"] = "No se seleccionó ningún archivo.";
        }

        return RedirectToAction("Detalles", new { id = licitacionId });
    }

    // [GET] Carga la lista de contratistas para el modal "Asignar Ganador"
    public async Task<IActionResult> _AsignarGanadorPartial(int licitacionId)
    {
        // Esta vista parcial solo necesita la lista de TODOS los contratistas
        var contratistas = await _context.Contratistas
                                         .OrderBy(c => c.RazonSocial)
                                         .ToListAsync();

        ViewBag.LicitacionId = licitacionId;
        // (Crearemos la vista "_AsignarGanadorPartial.cshtml" en el siguiente paso)
        return PartialView("_AsignarGanadorPartial", contratistas);
    }

    // [POST] Asigna al ganador (llamado desde el modal)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AsignarGanador(int licitacionId, int contratistaGanadorId)
    {
        var licitacion = await _context.Licitaciones.FindAsync(licitacionId);
        if (licitacion == null) return NotFound();

        var contratista = await _context.Contratistas.FindAsync(contratistaGanadorId);
        if (contratista == null)
        {
            TempData["Error"] = "El contratista seleccionado no es válido.";
            return RedirectToAction("Detalles", new { id = licitacionId });
        }

        if (licitacion.Estado != "Adjudicada")
        {
            licitacion.ContratistaGanadorId = contratistaGanadorId;
            _context.Update(licitacion);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Se ha asignado a '{contratista.RazonSocial}' como ganador.";
        }

        return RedirectToAction("Detalles", new { id = licitacionId });
    }

    // [POST] Manda el proyecto a la fase de Ejecución (Modo Control)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MandarAEjecutar(int licitacionId)
    {
        const int faseLicitacionId = 4;
        const int faseEjecucionId = 5;

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var licitacion = await _context.Licitaciones
                                           .Include(l => l.Proyecto)
                                           .Include(l => l.LicitacionDocumentos) // Cargamos documentos para validar
                                           .FirstOrDefaultAsync(l => l.Id == licitacionId);

            if (licitacion == null) return NotFound();

            // --- VALIDACIONES DE CONTROL ---
            if (licitacion.ContratistaGanadorId == null)
            {
                TempData["Error"] = "No se puede mandar a ejecutar. Debe asignar un contratista ganador primero.";
                return RedirectToAction("Detalles", new { id = licitacionId });
            }
            // Validamos que exista al menos el contrato (basado en tu diagrama)
            if (!licitacion.LicitacionDocumentos.Any(d => d.TipoDocumento == "Contrato"))
            {
                TempData["Error"] = "No se puede mandar a ejecutar. Debe subir el documento del 'Contrato' firmado.";
                return RedirectToAction("Detalles", new { id = licitacionId });
            }
            if (licitacion.Proyecto.IdFaseFk != faseLicitacionId)
            {
                TempData["Error"] = "El proyecto no se encuentra en la fase de licitación.";
                return RedirectToAction("Detalles", new { id = licitacionId });
            }

            // 1. Actualizar estado de licitación
            licitacion.Estado = "Adjudicada";
            _context.Licitaciones.Update(licitacion);

            // 2. Mover el proyecto
            var proyecto = licitacion.Proyecto;
            proyecto.IdFaseFk = faseEjecucionId;
            _context.Proyectos.Update(proyecto);

            // 3. Registrar historial
            var historial = new HistorialFase
            {
                ProyectoId = proyecto.Id,
                FaseAnteriorId = faseLicitacionId,
                FaseNuevaId = faseEjecucionId,
                FechaCambio = DateTime.Now,
                Comentario = "Adjudicación Directa registrada. El proyecto avanza a la fase de ejecución.",
                TipoCambio = "Aprobado"
            };
            _context.HistorialFases.Add(historial);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = $"El proyecto '{proyecto.NombreProyecto}' ha sido adjudicado y movido a 'Ejecución'.";
            return RedirectToAction("Index", "PanelDeFases");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            TempData["Error"] = "Ocurrió un error al intentar adjudicar la licitación.";
            return RedirectToAction("Detalles", new { id = licitacionId });
        }
    }

}